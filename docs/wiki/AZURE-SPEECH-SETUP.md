# Azure Speech Service - Setup and Configuration

**Last Updated:** 2025-02-04

---

## 📋 Overview

MOBAflow supports **Azure Cognitive Services Speech** for high-quality Text-to-Speech (TTS) announcements. With Azure Speech, you can generate realistic station announcements like "Next stop: Hamburg Central Station".

### Why Azure Speech?

| Feature | Windows SAPI | Azure Speech |
|---------|--------------|--------------|
| **Voice Quality** | Robotic | Natural (Neural Voices) |
| **Voices Available** | ~3-5 per language | 100+ per language |
| **Cost** | Free (offline) | Free up to 500K chars/month |
| **Internet** | Not required | Required |
| **Latency** | Instant | ~200-500ms |

---

## 🆓 Azure Free Tier

> **Important:** Azure Speech is **free** for small to medium usage!

### Free Tier F0 - Limits

| Resource | Limit (per month) |
|----------|-------------------|
| **Text-to-Speech (Neural)** | 500,000 characters |
| **Speech-to-Text** | 5 hours audio |
| **Requests per second** | 20 |

**Example calculation:**
- Average announcement: ~50 characters
- Free Tier allows: 500,000 / 50 = **10,000 announcements/month**
- This is sufficient for intensive model railroad sessions!

---

## 🚀 Step-by-Step Setup

### 1. Create an Azure Account

1. Go to [azure.microsoft.com/free](https://azure.microsoft.com/free/)
2. Click **"Start free"**
3. Sign in with a Microsoft account (or create one)
4. Verify your identity (credit card will NOT be charged in Free Tier!)

### 2. Create Speech Service

1. Sign in to the [Azure Portal](https://portal.azure.com)
2. Click **"Create a resource"** (+ icon top left)
3. Search for **"Speech"**
4. Select **"Speech"** (by Microsoft)
5. Click **"Create"**

### 3. Configure the Service

| Field | Value |
|-------|-------|
| **Subscription** | Your Azure subscription |
| **Resource group** | "mobaflow-rg" (create new) |
| **Region** | "Germany West Central" (for low latency in DE) |
| **Name** | "mobaflow-speech" |
| **Pricing tier** | **Free F0** (free!) |

6. Click **"Review + create"**
7. Click **"Create"**
8. Wait for deployment to complete (~1-2 minutes)

### 4. Get API Key

1. Go to your newly created Speech resource
2. Click **"Keys and Endpoint"** in the left menu
3. Copy:
   - **KEY 1** (or KEY 2) → This is your API key
   - **Region** → e.g., "germanywestcentral"

### 5. Configure MOBAflow

Open `WinUI\appsettings.json` and enter the values:

```json
{
  "Speech": {
    "Key": "YOUR-API-KEY-HERE",
    "Region": "germanywestcentral",
    "SpeakerEngineName": "Azure Cognitive Services",
    "VoiceName": "de-DE-KatjaNeural",
    "Rate": -1
  }
}
```

### 6. Select a Voice

Available German Neural Voices:

| Voice Name | Gender | Style |
|------------|--------|-------|
| `de-DE-KatjaNeural` | Female | Neutral, professional |
| `de-DE-ConradNeural` | Male | Friendly |
| `de-DE-AmalaNeural` | Female | Warm |
| `de-DE-BerndNeural` | Male | Authoritative |
| `de-DE-ChristophNeural` | Male | News anchor |
| `de-DE-ElkeNeural` | Female | Mature |
| `de-DE-GiselaNeural` | Female | Childlike |

All available voices: [Azure Voice Gallery](https://learn.microsoft.com/azure/ai-services/speech-service/language-support?tabs=tts)

---

## ⚙️ Configuration Options

### appsettings.json - Speech Section

```json
{
  "Speech": {
    "Key": "your-azure-speech-key",
    "Region": "germanywestcentral",
    "SpeakerEngineName": "Azure Cognitive Services",
    "VoiceName": "de-DE-KatjaNeural",
    "Rate": -1
  }
}
```

| Parameter | Description | Values |
|-----------|-------------|--------|
| `Key` | Azure Speech API Key | 32-character hex string |
| `Region` | Azure Region | `germanywestcentral`, `westeurope`, etc. |
| `SpeakerEngineName` | Engine selection | `"Azure Cognitive Services"` or `"Windows SAPI"` |
| `VoiceName` | Voice | `de-DE-KatjaNeural`, etc. |
| `Rate` | Speaking rate | `-10` (slow) to `+10` (fast), `-1` = default |

### Fallback to Windows SAPI

If Azure is unavailable (no internet, invalid key), MOBAflow automatically falls back to Windows SAPI.

To permanently use Windows SAPI:

```json
{
  "Speech": {
    "SpeakerEngineName": "Windows SAPI",
    "VoiceName": "Microsoft Hedda Desktop"
  }
}
```

---

## 🔒 Security

> ⚠️ **IMPORTANT:** Never commit your API key to Git!

### Best Practices

1. **Add appsettings.json to .gitignore**
2. **Commit appsettings.template.json** without secrets
3. **Use User Secrets** for local development:
   ```bash
   dotnet user-secrets set "Speech:Key" "your-key-here"
   ```
4. **Environment Variables** for production:
   ```bash
   set SPEECH__KEY=your-key-here
   ```

### If Your Key is Compromised

1. Go to Azure Portal
2. Navigate to your Speech resource
3. Click "Keys and Endpoint"
4. Click **"Regenerate Key 1"**
5. Update your local key

---

## 🧪 Testing

### In MOBAflow

1. Open MOBAflow
2. Go to **Settings**
3. Scroll to **"Text-to-Speech"** section
4. Click **"Test Speech"**
5. You should hear "This is a test of the speech output"

### Via Azure Portal

1. Go to [Azure Speech Studio](https://speech.microsoft.com/)
2. Sign in
3. Select "Text to Speech"
4. Test different voices

---

## 🔧 Troubleshooting

### Problem: "Speech key is invalid"

**Cause:** API key is incorrect or expired.

**Solution:**
1. Check if key was copied correctly (no spaces)
2. Generate a new key in Azure Portal
3. Ensure region is correct

### Problem: "No audio output"

**Cause:** Audio device not available.

**Solution:**
1. Check Windows audio settings
2. Ensure an output device is selected
3. Test with other applications

### Problem: "Quota exceeded"

**Cause:** Free Tier limit reached.

**Solution:**
1. Wait until next month (limit resets)
2. Upgrade to Standard tier (paid)
3. Use Windows SAPI as fallback

### Problem: "Network error"

**Cause:** No internet connection.

**Solution:**
1. Check internet connection
2. Check firewall settings (allow HTTPS to *.microsoft.com)
3. MOBAflow automatically uses Windows SAPI as fallback

---

## 💰 Costs When Upgrading

If you exceed the Free Tier or need premium features:

| Tier | Cost (EUR) | Characters/Month |
|------|------------|------------------|
| **Free F0** | €0.00 | 500,000 |
| **Standard S0** | ~€15.00 per 1M characters | Unlimited |

**Tip:** For normal model railroad usage, Free Tier is completely sufficient!

---

## 📚 Further Reading

- [Azure Speech Service Documentation](https://learn.microsoft.com/azure/ai-services/speech-service/)
- [Azure Voice Gallery](https://learn.microsoft.com/azure/ai-services/speech-service/language-support?tabs=tts)
- [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/)
- [Speech SDK GitHub](https://github.com/Azure-Samples/cognitive-services-speech-sdk)

---

*Last updated: 2025-02-04*
