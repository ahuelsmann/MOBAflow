#include "ProvisioningState.h"

namespace MobaDisplay::Provisioning
{
void StateMachine::Boot(bool hasActiveCredentials, bool hasOwner)
{
    activeCredentials_ = hasActiveCredentials;
    ownerBound_ = hasOwner;
    pendingCredentials_ = false;
    ClearSession();
    state_ = hasActiveCredentials ? State::Operational : State::AwaitingActivation;
}

bool StateMachine::BeginActivation(uint32_t nowMs)
{
    if ((state_ != State::AwaitingActivation && state_ != State::Operational && state_ != State::Offline)
        || (cooldownUntilMs_ != 0 && IsBefore(nowMs, cooldownUntilMs_)))
        return false;

    windowStartedAtMs_ = nowMs;
    authenticationFailures_ = 0;
    pendingCredentials_ = false;
    ClearSession();
    state_ = State::WindowOpen;
    return true;
}

bool StateMachine::AuthenticateSession()
{
    if (state_ != State::WindowOpen)
        return false;

    sessionAuthenticated_ = true;
    return true;
}

bool StateMachine::SessionAuthenticated() const
{
    return state_ == State::WindowOpen && sessionAuthenticated_;
}

bool StateMachine::EnrollOwner()
{
    if (state_ != State::WindowOpen || !sessionAuthenticated_ || ownerBound_)
        return false;

    ownerBound_ = true;
    return true;
}

bool StateMachine::SubmitCredentials(const CredentialView& credentials)
{
    if (state_ != State::WindowOpen || !sessionAuthenticated_ || !ownerBound_ || !IsValidCredentials(credentials))
        return false;

    pendingCredentials_ = true;
    state_ = State::PendingConnection;
    return true;
}

bool StateMachine::MarkStationUsable(uint32_t nowMs)
{
    if (state_ != State::PendingConnection || !pendingCredentials_)
        return false;

    handoverStartedAtMs_ = nowMs;
    state_ = State::AwaitingHandover;
    return true;
}

bool StateMachine::ConfirmHandover(uint32_t nowMs)
{
    if (state_ != State::AwaitingHandover
        || IsBefore(nowMs, handoverStartedAtMs_)
        || HasElapsed(nowMs, handoverStartedAtMs_, kHandoverDurationMs))
        return false;

    state_ = State::PromotionPending;
    return true;
}

bool StateMachine::CompletePromotion()
{
    if (state_ != State::PromotionPending || !pendingCredentials_)
        return false;

    activeCredentials_ = true;
    pendingCredentials_ = false;
    ClearSession();
    state_ = State::Operational;
    return true;
}

bool StateMachine::RecordAuthenticationFailure(uint32_t nowMs)
{
    if (state_ != State::WindowOpen)
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
    return state_ == State::WindowOpen && sessionAuthenticated_ && ownerBound_ && signatureValid;
}

void StateMachine::CloseWindow(bool activeNetworkVerified)
{
    pendingCredentials_ = false;
    CloseToStableState(activeNetworkVerified && activeCredentials_);
}

void StateMachine::Tick(uint32_t nowMs)
{
    if ((state_ == State::WindowOpen || state_ == State::PendingConnection || state_ == State::AwaitingHandover)
        && (HasElapsed(nowMs, windowStartedAtMs_, kWindowDurationMs)
            || (state_ == State::AwaitingHandover && HasElapsed(nowMs, handoverStartedAtMs_, kHandoverDurationMs))))
    {
        pendingCredentials_ = false;
        CloseToStableState(activeCredentials_);
    }
}

bool StateMachine::HasElapsed(uint32_t nowMs, uint32_t startMs, uint32_t durationMs)
{
    return static_cast<uint32_t>(nowMs - startMs) >= durationMs;
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
    state_ = activeNetworkVerified ? State::Operational : State::AwaitingActivation;
}
}
