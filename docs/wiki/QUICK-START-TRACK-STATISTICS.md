# Quick Start: Track Statistics Setup

> **For Open Source Users** - Getting started with MOBAflow Track Statistics without creating a full Solution file.

## 📋 Overview

MOBAflow's **Track Statistics** feature tracks lap counts and timing for each feedback point (InPort) on your model railway layout. You can use it **without loading a Solution file** by simply configuring the number of feedback points you have.

---

## 🚀 Quick Setup (3 Steps)

### **Step 1: Configure Number of Feedback Points**

Edit the `appsettings.json` file in MOBAflow, or use the settings UI of your app:

#### **MOBAflow (Desktop)**
File: `MOBAflow/appsettings.json`

```json
"Counter": {
  "CountOfFeedbackPoints": 3,  // Change this to match your layout
  "TargetLapCount": 10,
  "UseTimerFilter": true,
  "TimerIntervalSeconds": 10.0
}
```

#### **MOBAsmart (Mobile)**
These values are stored in the app settings/preferences. Use the settings UI instead of editing a repo file.

### **Step 2: Understand InPort Mapping**

MOBAflow uses a simple 1:1 mapping:

| InPort Number | Feedback Point | Track Statistics Display |
|---------------|----------------|--------------------------|
| **1** | Feedback Point 1 | "Feedback Point 1" |
| **2** | Feedback Point 2 | "Feedback Point 2" |
| **3** | Feedback Point 3 | "Feedback Point 3" |
| **0** | (Special) | Disabled / Not in use |

**Example:** If you have 3 feedback points on your layout → Set `CountOfFeedbackPoints = 3`

### **Step 3: Connect to Z21 & Start Monitoring**

1. Start MOBAflow
2. Connect to your Z21 digital command station
3. Navigate to **Overview** page
4. **Track Statistics** section will show:
   - Feedback Point 1 (InPort 1)
   - Feedback Point 2 (InPort 2)
   - Feedback Point 3 (InPort 3)

**Done!** 🎉

---

## 🎛️ Change Settings via UI (MOBAflow Desktop)

Instead of editing JSON files, you can use the **Settings Page**:

1. Open **Settings** from navigation menu
2. Expand **"Track Statistics"** section
3. Change **"Number of Feedback Points"** using the NumberBox
4. Click **Save** (automatic via app settings)
5. **Restart** MOBAflow to apply changes

---

## 📊 What Happens When Train Passes Feedback Point?

When a train triggers a feedback sensor:

```
Train passes InPort 2
     ↓
Z21 sends feedback event
     ↓
MOBAflow updates "Feedback Point 2":
  • Counter: 0/10 → 1/10
  • Last Lap Time: 00:02:15 (time since last pass)
  • Last Seen: 14:32:45
```

**UI updates automatically** on all platforms! ✨

---

## 🔧 Advanced Configuration

### **Timer Filter (Prevent Double Counts)**

Long trains (e.g., 16-axle freighters) may trigger the sensor multiple times. Use the **Timer Filter** to ignore duplicate counts:

```json
"UseTimerFilter": true,
"TimerIntervalSeconds": 10.0  // Ignore counts within 10 seconds
```

### **Target Lap Count**

Set the target number of laps for all tracks:

```json
"TargetLapCount": 10  // Progress bar shows X/10 laps
```

---

## ❓ FAQ

### **Q: I have 0 feedback points configured. Will it crash?**
**A:** No! MOBAflow will simply show: *"CountOfFeedbackPoints is 0 - no track statistics initialized."*

### **Q: Can I use InPort 4 if `CountOfFeedbackPoints = 3`?**
**A:** Yes! You can configure Journeys/Workflows with InPort 4, but Track Statistics won't show it (unless you increase the setting to 4).

### **Q: What does InPort=0 mean in a Journey?**
**A:** **Disabled** - This Journey won't trigger automatically via feedback. You can still trigger it manually via commands.

### **Q: Do I need a Solution file?**
**A:** **No!** Track Statistics work standalone with just the `CountOfFeedbackPoints` setting. Solution files are optional (they add named feedback points and advanced features like Journeys/Workflows).

---

## 🎯 Next Steps

Once you're comfortable with Track Statistics, explore:

1. **Journeys** - Automate station announcements based on feedback points
2. **Workflows** - Execute custom actions (speed changes, announcements, etc.)
3. **Track Plan Editor** - Visual layout designer with feedback point assignment

**Happy Railroading!** 🚂

---

## 📖 Related Documentation

- [Architecture Overview](../ARCHITECTURE.md)
- [Solution File Format & Validation](../JSON-VALIDATION.md)
- [Z21 Connection Guide](../../README.md)
