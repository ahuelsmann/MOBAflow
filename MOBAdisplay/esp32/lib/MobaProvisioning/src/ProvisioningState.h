#pragma once

#include <cstddef>
#include <cstdint>

namespace MobaDisplay::Provisioning
{
constexpr size_t kMaxSsidBytes = 32;
constexpr size_t kMinPassphraseBytes = 8;
constexpr size_t kMaxPassphraseBytes = 63;
constexpr uint32_t kWindowDurationMs = 10U * 60U * 1000U;
constexpr uint8_t kMaxAuthenticationFailures = 10;
constexpr uint32_t kCooldownDurationMs = 60U * 1000U;
constexpr uint32_t kHandoverDurationMs = 60U * 1000U;

enum class State : uint8_t
{
    Unprovisioned,
    AwaitingActivation,
    Operational,
    WindowOpen,
    PendingConnection,
    AwaitingHandover,
    PromotionPending,
    Offline
};

struct CredentialView
{
    const uint8_t* ssid = nullptr;
    size_t ssidLength = 0;
    const uint8_t* passphrase = nullptr;
    size_t passphraseLength = 0;
};

class StateMachine final
{
public:
    StateMachine() = default;

    void Boot(bool hasActiveCredentials, bool hasOwner);
    bool BeginActivation(uint32_t nowMs);
    bool AuthenticateSession();
    bool SessionAuthenticated() const;
    bool EnrollOwner();
    bool SubmitCredentials(const CredentialView& credentials);
    bool MarkStationUsable(uint32_t nowMs);
    bool ConfirmHandover(uint32_t nowMs);
    bool CompletePromotion();
    bool RecordAuthenticationFailure(uint32_t nowMs);
    bool AuthorizeOwnerAction(bool signatureValid) const;
    void CloseWindow(bool activeNetworkVerified);
    void Tick(uint32_t nowMs);

    State GetState() const { return state_; }
    bool HasOwner() const { return ownerBound_; }
    bool HasActiveCredentials() const { return activeCredentials_; }
    bool HasPendingCredentials() const { return pendingCredentials_; }
    bool IsSessionAuthenticated() const { return sessionAuthenticated_; }
    uint8_t AuthenticationFailures() const { return authenticationFailures_; }
    uint32_t WindowStartedAt() const { return windowStartedAtMs_; }
    uint32_t CooldownUntil() const { return cooldownUntilMs_; }

private:
    static bool HasElapsed(uint32_t nowMs, uint32_t startMs, uint32_t durationMs);
    static bool IsBefore(uint32_t nowMs, uint32_t deadlineMs);
    static bool IsValidCredentials(const CredentialView& credentials);
    void ClearSession();
    void CloseToStableState(bool activeNetworkVerified);

    State state_ = State::Unprovisioned;
    bool ownerBound_ = false;
    bool activeCredentials_ = false;
    bool pendingCredentials_ = false;
    bool sessionAuthenticated_ = false;
    uint8_t authenticationFailures_ = 0;
    uint32_t windowStartedAtMs_ = 0;
    uint32_t cooldownUntilMs_ = 0;
    uint32_t handoverStartedAtMs_ = 0;
};
}
