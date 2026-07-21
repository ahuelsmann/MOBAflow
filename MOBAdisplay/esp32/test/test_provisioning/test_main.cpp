#include <ProvisioningState.h>
#include <unity.h>

#include <cstdint>

using MobaDisplay::Provisioning::CredentialView;
using MobaDisplay::Provisioning::State;
using MobaDisplay::Provisioning::StateMachine;

namespace
{
CredentialView Credentials()
{
    static const uint8_t ssid[] = "MOBAflow-test";
    static const uint8_t passphrase[] = "correct-horse";
    CredentialView credentials;
    credentials.ssid = ssid;
    credentials.ssidLength = sizeof(ssid) - 1;
    credentials.passphrase = passphrase;
    credentials.passphraseLength = sizeof(passphrase) - 1;
    return credentials;
}
}

void setUp() {}
void tearDown() {}

void TestBootRequiresPhysicalActivation()
{
    StateMachine machine;
    machine.Boot(false, false);

    TEST_ASSERT_EQUAL_UINT8(static_cast<uint8_t>(State::AwaitingActivation), static_cast<uint8_t>(machine.GetState()));
    TEST_ASSERT_FALSE(machine.SessionAuthenticated());
    TEST_ASSERT_TRUE(machine.BeginActivation(100));
    TEST_ASSERT_EQUAL_UINT8(static_cast<uint8_t>(State::WindowOpen), static_cast<uint8_t>(machine.GetState()));
}

void TestEnrollmentPrecedesCredentialPromotion()
{
    StateMachine machine;
    machine.Boot(false, false);
    TEST_ASSERT_TRUE(machine.BeginActivation(100));
    TEST_ASSERT_TRUE(machine.SessionAuthenticated());
    TEST_ASSERT_FALSE(machine.SubmitCredentials(Credentials()));
    TEST_ASSERT_TRUE(machine.EnrollOwner());
    TEST_ASSERT_TRUE(machine.SubmitCredentials(Credentials()));
    TEST_ASSERT_TRUE(machine.MarkStationUsable(200));
    TEST_ASSERT_FALSE(machine.ConfirmHandover(199));
    TEST_ASSERT_TRUE(machine.ConfirmHandover(200));
    TEST_ASSERT_TRUE(machine.CompletePromotion());
    TEST_ASSERT_EQUAL_UINT8(static_cast<uint8_t>(State::Operational), static_cast<uint8_t>(machine.GetState()));
}

void TestOwnerAuthorizationCannotUsePhysicalActivationAlone()
{
    StateMachine machine;
    machine.Boot(true, true);
    TEST_ASSERT_TRUE(machine.BeginActivation(100));
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
    TEST_ASSERT_TRUE(machine.SessionAuthenticated());
    TEST_ASSERT_TRUE(machine.SubmitCredentials(Credentials()));
    machine.CloseWindow(true);
    TEST_ASSERT_EQUAL_UINT8(static_cast<uint8_t>(State::Operational), static_cast<uint8_t>(machine.GetState()));
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

    TEST_ASSERT_EQUAL_UINT8(static_cast<uint8_t>(State::Operational), static_cast<uint8_t>(machine.GetState()));
    TEST_ASSERT_FALSE(machine.BeginActivation(200));
    TEST_ASSERT_TRUE(machine.BeginActivation(109 + 60000 + 1));
}

void TestWindowTimeoutClosesWithoutOpeningAccess()
{
    StateMachine machine;
    machine.Boot(false, false);
    TEST_ASSERT_TRUE(machine.BeginActivation(0xFFFFFF00U));
    TEST_ASSERT_TRUE(machine.SessionAuthenticated());
    machine.Tick(0xFFFFFF00U + 600000U);
    TEST_ASSERT_EQUAL_UINT8(static_cast<uint8_t>(State::AwaitingActivation), static_cast<uint8_t>(machine.GetState()));
    TEST_ASSERT_FALSE(machine.IsSessionAuthenticated());
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
    return UNITY_END();
}
