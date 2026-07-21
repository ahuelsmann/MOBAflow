#pragma once

#include <cstddef>

#if defined(ESP_PLATFORM)
#include <esp_err.h>
#include <protocomm.h>
#else
using esp_err_t = int;
constexpr esp_err_t ESP_OK = 0;
constexpr esp_err_t ESP_ERR_INVALID_ARG = -1;
constexpr esp_err_t ESP_ERR_NOT_SUPPORTED = -2;

struct protocomm;
using protocomm_t = protocomm;
using protocomm_req_handler_t = void (*)(void*);
#endif

namespace MobaDisplay::Provisioning
{
class Security2Transport final
{
public:
    static constexpr char kUsername[] = "mobaflow-provisioning-v1";
    static constexpr size_t kUsernameLength = sizeof(kUsername) - 1;

    Security2Transport() = default;
    ~Security2Transport();

    Security2Transport(const Security2Transport&) = delete;
    Security2Transport& operator=(const Security2Transport&) = delete;

    esp_err_t Start(const char* setupSecret, protocomm_req_handler_t requestHandler, void* privateData);
    esp_err_t Stop();
    bool IsRunning() const { return protocomm_ != nullptr; }

private:
    protocomm_t* protocomm_ = nullptr;
    char* salt_ = nullptr;
    char* verifier_ = nullptr;
    int verifierLength_ = 0;
};

static_assert(Security2Transport::kUsernameLength == 24, "The public Security 2 username is a protocol constant.");
}
