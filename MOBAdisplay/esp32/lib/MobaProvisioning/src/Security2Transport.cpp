#include "Security2Transport.h"

#include <cstdlib>
#include <cstring>

#if defined(ESP_PLATFORM)
#include <esp_srp.h>
#include <freertos/FreeRTOS.h>
#include <protocomm_httpd.h>
#include <protocomm_security2.h>
#endif

namespace MobaDisplay::Provisioning
{
constexpr char Security2Transport::kUsername[];

Security2Transport::~Security2Transport()
{
    Stop();
}

esp_err_t Security2Transport::Start(const char* setupSecret, protocomm_req_handler_t requestHandler)
{
#if !defined(ESP_PLATFORM)
    (void)setupSecret;
    (void)requestHandler;
    return ESP_ERR_NOT_SUPPORTED;
#else
    if (protocomm_ != nullptr || setupSecret == nullptr || requestHandler == nullptr)
        return ESP_ERR_INVALID_ARG;

    const size_t secretLength = std::strlen(setupSecret);
    if (secretLength < 16 || secretLength > 63)
        return ESP_ERR_INVALID_ARG;

    esp_err_t result = esp_srp_gen_salt_verifier(
        kUsername,
        static_cast<int>(kUsernameLength),
        setupSecret,
        static_cast<int>(secretLength),
        &salt_,
        16,
        &verifier_,
        &verifierLength_);
    if (result != ESP_OK)
        return result;

    protocomm_security2_params_t securityParameters = {
        salt_,
        16,
        verifier_,
        static_cast<uint16_t>(verifierLength_)};

    protocomm_ = protocomm_new();
    if (protocomm_ == nullptr)
    {
        Stop();
        return ESP_ERR_NO_MEM;
    }

    result = protocomm_set_security(protocomm_, "rf02-session", &protocomm_security2, &securityParameters);
    if (result == ESP_OK)
        result = protocomm_set_version(protocomm_, "rf02-version", "v1");
    if (result == ESP_OK)
        result = protocomm_add_endpoint(protocomm_, "rf02-v1", requestHandler, nullptr);
    if (result == ESP_OK)
    {
        protocomm_httpd_config_t httpConfiguration = {};
        httpConfiguration.ext_handle_provided = false;
        httpConfiguration.data.config.port = 80;
        httpConfiguration.data.config.stack_size = 4096;
        httpConfiguration.data.config.task_priority = tskIDLE_PRIORITY + 5;
        result = protocomm_httpd_start(protocomm_, &httpConfiguration);
    }

    if (result != ESP_OK)
        Stop();
    return result;
#endif
}

esp_err_t Security2Transport::Stop()
{
#if !defined(ESP_PLATFORM)
    return ESP_OK;
#else
    esp_err_t result = ESP_OK;
    if (protocomm_ != nullptr)
    {
        result = protocomm_httpd_stop(protocomm_);
        protocomm_delete(protocomm_);
        protocomm_ = nullptr;
    }

    if (salt_ != nullptr)
    {
        std::memset(salt_, 0, 16);
        std::free(salt_);
        salt_ = nullptr;
    }
    if (verifier_ != nullptr)
    {
        std::memset(verifier_, 0, static_cast<size_t>(verifierLength_));
        std::free(verifier_);
        verifier_ = nullptr;
    }
    verifierLength_ = 0;
    return result;
#endif
}
}
