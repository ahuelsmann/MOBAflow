#include "ProvisioningState.h"

namespace MobaDisplay::Provisioning
{
constexpr State kAwaitingActivationState = static_cast<State>(1);
constexpr State kOperationalState = static_cast<State>(2);
constexpr State kWindowOpenState = static_cast<State>(3);
constexpr State kPendingConnectionState = static_cast<State>(4);
constexpr State kAwaitingHandoverState = static_cast<State>(5);
constexpr State kPromotionPendingState = static_cast<State>(6);
constexpr State kOfflineState = static_cast<State>(7);

void StateMachine::Boot(bool hasActiveCredentials, bool hasOwner)
{
    activeCredentials_ = hasActiveCredentials;
    ownerBound_ = hasOwner;
    pendingCredentials_ = false;
    ClearSession();
    state_ = hasActiveCredentials ? kOperationalState : kAwaitingActivationState;
}

bool StateMachine::BeginActivation(uint32_t nowMs)
{
    if ((state_ != kAwaitingActivationState && state_ != kOperationalState && state_ != kOfflineState)
        || (cooldownUntilMs_ != 0 && IsBefore(nowMs, cooldownUntilMs_)))
        return false;

    windowStartedAtMs_ = nowMs;
    authenticationFailures_ = 0;
    pendingCredentials_ = false;
    ClearSession();
    state_ = kWindowOpenState;
    return true;
}

bool StateMachine::AuthenticateSession()
{
    if (state_ != kWindowOpenState)
        return false;

    sessionAuthenticated_ = true;
    return true;
}

bool StateMachine::SessionAuthenticated() const
{
    return state_ == kWindowOpenState && sessionAuthenticated_;
}

bool StateMachine::EnrollOwner()
{
    if (state_ != kWindowOpenState || !sessionAuthenticated_ || ownerBound_)
        return false;

    ownerBound_ = true;
    return true;
}

bool StateMachine::SubmitCredentials(const CredentialView& credentials)
{
    if (state_ != kWindowOpenState || !sessionAuthenticated_ || !ownerBound_ || !IsValidCredentials(credentials))
        return false;

    pendingCredentials_ = true;
    state_ = kPendingConnectionState;
    return true;
}

bool StateMachine::MarkStationUsable(uint32_t nowMs)
{
    if (state_ != kPendingConnectionState || !pendingCredentials_)
        return false;

    handoverStartedAtMs_ = nowMs;
    state_ = kAwaitingHandoverState;
    return true;
}

bool StateMachine::ConfirmHandover(uint32_t nowMs)
{
    if (state_ != kAwaitingHandoverState
        || IsBefore(nowMs, handoverStartedAtMs_)
        || HasElapsed(nowMs, handoverStartedAtMs_, kHandoverDurationMs))
        return false;

    state_ = kPromotionPendingState;
    return true;
}

bool StateMachine::CompletePromotion()
{
    if (state_ != kPromotionPendingState || !pendingCredentials_)
        return false;

    activeCredentials_ = true;
    pendingCredentials_ = false;
    ClearSession();
    state_ = kOperationalState;
    return true;
}

bool StateMachine::RecordAuthenticationFailure(uint32_t nowMs)
{
    if (state_ != kWindowOpenState)
        return false;

    if (authenticationFailures_ < kMaxAuthenticationFailures)
        ++authenticationFailures_;

    if (authenticationFailures_ >= kMaxAuthenticationFailures)
    {
        cooldownUntilMs_ = nowMs + kCooldownDurationMs;
        CloseToStableState(activeCredentials_);
    }

    return true;
}

bool StateMachine::AuthorizeOwnerAction(bool signatureValid) const
{
    return state_ == kWindowOpenState && sessionAuthenticated_ && ownerBound_ && signatureValid;
}

void StateMachine::CloseWindow(bool activeNetworkVerified)
{
    pendingCredentials_ = false;
    CloseToStableState(activeNetworkVerified && activeCredentials_);
}

void StateMachine::Tick(uint32_t nowMs)
{
    if ((state_ == kWindowOpenState || state_ == kPendingConnectionState || state_ == kAwaitingHandoverState)
        && (HasElapsed(nowMs, windowStartedAtMs_, kWindowDurationMs)
            || (state_ == kAwaitingHandoverState && HasElapsed(nowMs, handoverStartedAtMs_, kHandoverDurationMs))))
    {
        pendingCredentials_ = false;
        CloseToStableState(activeCredentials_);
    }
}

bool StateMachine::HasElapsed(uint32_t nowMs, uint32_t startMs, uint32_t durationMs)
{
    return nowMs - startMs >= durationMs;
}

bool StateMachine::IsBefore(uint32_t nowMs, uint32_t deadlineMs)
{
    const uint32_t elapsed = nowMs - deadlineMs;
    return (elapsed & 0x80000000U) != 0U;
}

bool StateMachine::IsValidCredentials(const CredentialView& credentials)
{
    return credentials.ssid != nullptr && credentials.ssidLength > 0 && credentials.ssidLength <= kMaxSsidBytes
        && credentials.passphrase != nullptr && credentials.passphraseLength >= kMinPassphraseBytes
        && credentials.passphraseLength <= kMaxPassphraseBytes;
}

void StateMachine::ClearSession()
{
    sessionAuthenticated_ = false;
}

void StateMachine::CloseToStableState(bool activeNetworkVerified)
{
    ClearSession();
    state_ = activeNetworkVerified ? kOperationalState : kAwaitingActivationState;
}
}
