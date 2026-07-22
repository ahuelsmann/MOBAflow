#include <ProvisioningState.h>
#include <unity.h>

#include <cstdint>
#include <cstring>

using MobaDisplay::Provisioning::CredentialView;
using MobaDisplay::Provisioning::State;
using MobaDisplay::Provisioning::StateMachine;

namespace
{
CredentialView Credentials()
{
    static const uint8_t ssid[] = "MOBAflow-test";
    // Deterministic non-secret bytes keep state-machine tests independent of real credentials.
    static const uint8_t passphrase[] = {1, 2, 3, 4, 5, 6, 7, 8};
    CredentialView credentials;
    credentials.ssid = ssid;
    credentials.ssidLength = sizeof(ssid) - 1;
    credentials.passphrase = passphrase;
    credentials.passphraseLength = sizeof(passphrase);
    return credentials;
}

uint8_t StateValue(State value)
{
    switch (value)
    {
    case State::Unprovisioned:
        return 0;
    case State::AwaitingActivation:
        return 1;
    case State::Operational:
        return 2;
    case State::WindowOpen:
        return 3;
    case State::PendingConnection:
        return 4;
    case State::AwaitingHandover:
        return 5;
    case State::PromotionPending:
        return 6;
    case State::Offline:
        return 7;
    }

    return 0;
}
}

void setUp()
{
    // Unity requires these hooks even though the provisioning tests need no fixture setup.
}

void tearDown()
{
    // Unity requires these hooks even though the provisioning tests need no fixture cleanup.
}

void TestBootRequiresPhysicalActivation()
{
    StateMachine machine;
    machine.Boot(false, false);

    TEST_ASSERT_EQUAL_UINT8(StateValue(State::AwaitingActivation), StateValue(machine.GetState()));
    TEST_ASSERT_FALSE(machine.SessionAuthenticated());
    TEST_ASSERT_TRUE(machine.BeginActivation(100));
    TEST_ASSERT_EQUAL_UINT8(StateValue(State::WindowOpen), StateValue(machine.GetState()));
    TEST_ASSERT_FALSE(machine.SessionAuthenticated());
}

void TestEnrollmentPrecedesCredentialPromotion()
{
    StateMachine machine;
    machine.Boot(false, false);
    TEST_ASSERT_TRUE(machine.BeginActivation(100));
    TEST_ASSERT_TRUE(machine.AuthenticateSession());
    TEST_ASSERT_TRUE(machine.SessionAuthenticated());
    TEST_ASSERT_FALSE(machine.SubmitCredentials(Credentials()));
    TEST_ASSERT_TRUE(machine.EnrollOwner());
    TEST_ASSERT_TRUE(machine.SubmitCredentials(Credentials()));
    TEST_ASSERT_TRUE(machine.MarkStationUsable(200));
    TEST_ASSERT_FALSE(machine.ConfirmHandover(199));
    TEST_ASSERT_TRUE(machine.ConfirmHandover(200));
    TEST_ASSERT_TRUE(machine.CompletePromotion());
    TEST_ASSERT_EQUAL_UINT8(StateValue(State::Operational), StateValue(machine.GetState()));
}

void TestOwnerAuthorizationCannotUsePhysicalActivationAlone()
{
    StateMachine machine;
    machine.Boot(true, true);
    TEST_ASSERT_TRUE(machine.BeginActivation(100));
    TEST_ASSERT_TRUE(machine.AuthenticateSession());
    TEST_ASSERT_FALSE(machine.AuthorizeOwnerAction(false));
    TEST_ASSERT_TRUE(machine.SessionAuthenticated());
    TEST_ASSERT_FALSE(machine.AuthorizeOwnerAction(false));
    TEST_ASSERT_TRUE(machine.AuthorizeOwnerAction(true));
}

void TestFailedRotationRetainsActiveNetwork()
{
    StateMachine machine;
    machine.Boot(true, true);
    TEST_ASSERT_TRUE(machine.BeginActivation(100));
    TEST_ASSERT_TRUE(machine.AuthenticateSession());
    TEST_ASSERT_TRUE(machine.SubmitCredentials(Credentials()));
    machine.CloseWindow(true);
    TEST_ASSERT_EQUAL_UINT8(StateValue(State::Operational), StateValue(machine.GetState()));
    TEST_ASSERT_TRUE(machine.HasActiveCredentials());
    TEST_ASSERT_FALSE(machine.HasPendingCredentials());
}

void TestAuthenticationLimitEnforcesCooldown()
{
    StateMachine machine;
    machine.Boot(true, true);
    TEST_ASSERT_TRUE(machine.BeginActivation(100));
    for (uint8_t attempt = 0; attempt < 10; ++attempt)
        TEST_ASSERT_TRUE(machine.RecordAuthenticationFailure(100 + attempt));

    TEST_ASSERT_EQUAL_UINT8(StateValue(State::Operational), StateValue(machine.GetState()));
    TEST_ASSERT_FALSE(machine.BeginActivation(200));
    TEST_ASSERT_TRUE(machine.BeginActivation(109 + 60000 + 1));
}

void TestWindowTimeoutClosesWithoutOpeningAccess()
{
    StateMachine machine;
    machine.Boot(false, false);
    TEST_ASSERT_TRUE(machine.BeginActivation(0xFFFFFF00U));
    TEST_ASSERT_TRUE(machine.AuthenticateSession());
    machine.Tick(0xFFFFFF00U + 600000U);
    TEST_ASSERT_EQUAL_UINT8(StateValue(State::AwaitingActivation), StateValue(machine.GetState()));
    TEST_ASSERT_FALSE(machine.IsSessionAuthenticated());
}

void TestSessionReadDoesNotGrantAuthentication()
{
    StateMachine machine;
    machine.Boot(false, false);
    TEST_ASSERT_TRUE(machine.BeginActivation(100));
    TEST_ASSERT_FALSE(machine.SessionAuthenticated());
    TEST_ASSERT_TRUE(machine.AuthenticateSession());
    TEST_ASSERT_TRUE(machine.SessionAuthenticated());
}

void TestCredentialsRequireWpa2MinimumPassphraseLength()
{
    StateMachine machine;
    machine.Boot(false, false);
    TEST_ASSERT_TRUE(machine.BeginActivation(100));
    TEST_ASSERT_TRUE(machine.AuthenticateSession());
    TEST_ASSERT_TRUE(machine.EnrollOwner());

    static const uint8_t ssid[] = "MOBAflow-test";
    static const uint8_t shortPassphrase[] = {1, 2, 3, 4, 5, 6, 7};
    CredentialView credentials;
    credentials.ssid = ssid;
    credentials.ssidLength = sizeof(ssid) - 1;
    credentials.passphrase = shortPassphrase;
    credentials.passphraseLength = sizeof(shortPassphrase) - 1;
    TEST_ASSERT_FALSE(machine.SubmitCredentials(credentials));
    TEST_ASSERT_EQUAL_UINT8(StateValue(State::WindowOpen), StateValue(machine.GetState()));
}

int main(int, char**)
{
    UNITY_BEGIN();
    RUN_TEST(TestBootRequiresPhysicalActivation);
    RUN_TEST(TestEnrollmentPrecedesCredentialPromotion);
    RUN_TEST(TestOwnerAuthorizationCannotUsePhysicalActivationAlone);
    RUN_TEST(TestFailedRotationRetainsActiveNetwork);
    RUN_TEST(TestAuthenticationLimitEnforcesCooldown);
    RUN_TEST(TestWindowTimeoutClosesWithoutOpeningAccess);
    RUN_TEST(TestSessionReadDoesNotGrantAuthentication);
    RUN_TEST(TestCredentialsRequireWpa2MinimumPassphraseLength);
    return UNITY_END();
}
