# NitroDockX
---
### A Customizable RocketDock Clone for Windows 11

[![Demo Video](https://github.com/user-attachments/assets/171776b6-8597-42a0-8f8f-d7bb084738e6)](https://streamable.com/tg5p6e)

#### **NitroDockX** is a **modern, lightweight, and highly customizable** alternative to RocketDock, designed **exclusively for Windows 11**. Whether you're a power user, a gamer, or someone who loves a clean desktop, NitroDockX lets you **organize your apps, folders, and files in style**... <br><br>

## ✨ **Features**
---
### **Core Functionality**
✅  **Drag-and-Drop Support**: Add files, folders, or executables to the dock <br>
✅  **Multi-Monitor Support**: Position the dock on any screen edge <br>
✅  **Custom Icons**: Replace default icons with `.png` or `.ico` files <br>
✅  **Dynamic Icon Sizing**: Adjust icon size and spacing <br>
✅  **Smooth Animations**: Glow effects and hover animations <br>

### **UI Customization**
✅  **Opacity Control**: Adjust dock transparency <br>
✅  **Rounded Corners**: Modern, soft-edged design <br>
✅  **Themes/Skins**: Apply custom skins to change the dock’s appearance <br>
✅  **Dock Positioning**: Snap to any screen edge or position freely <br>
✅  **Edge Offset**: Fine-tune dock distance from screen edges <br>

### **Advanced Features**
✅  **Middle-Click Reordering**: Hold the middle mouse button and scroll to reorder icons <br>
✅  **Context Menus**: Right-click icons for:  <br>
  - **Properties**: Change icon appearance or file association.
  - **Remove Item**: Delete an icon.
  - **Clear .ini File**: Reset the dock’s configuration.
  - **Exit NitroDockX**: Close the application.
  
✅  **Startup Integration**: Launch NitroDockX automatically with Windows <br>
✅  **Shortcut Resolution**: Supports `.lnk` (Windows shortcut) files <br>

### **Technical Details**
✅  **Configuration File**: Settings saved to `NitroDockX.ini` <br>
✅  **Lightweight**: Built with **C# and .NET** for performance <br>
✅  **Open-Source**: Fully customizable and extensible <br> <br>

## 📌 **How to Use**
---
### **Adding Icons**
 #### 1. Drag and drop files, folders, or executables onto the dock
 #### 2. Right-click icons to customize or remove them

### **Configuring the Dock**

#### 1. Right-click the **Configuration Button** (shield icon).
#### 2. Adjustable User Settings: <br>
✅  Dock position (left/right/top/bottom) <br>
✅  Icon size, spacing, and glow color <br>
✅  Opacity and edge offsets <br>
✅  Skins/themes <br>

### **Resetting Settings**
> Use **"Clear .ini File"** in the icon context menu to reset the dock’s configuration: <br>
*This resets the .ini file and erases it. This is to cleanup configurations of removed buttons that may happen from time to time*
 <br>


## 🛠️ **Installation**
---
1. **Clone** the latest release of the repository: **https://github.com/ghostbyte420/NitroDockX.git**
2. **Open the NitroDockX.slnx in Visual Studio **2026** AND compile the solution
3. **Run** `<NitroDockX Project>/bin/net9.0-windows/NitroDockX.exe` <br> OR: move the files out of this folder and into a folder of your choice and then Run `<NitroDockX>`
4. **Customize** via the configuration panel. <br> <br>


## 🎨 **Customization**

### **Skins**
- Place skin folders in `NitroSkins/` (e.g., `NitroSkins/MySkin/01.png`).
- Select skins in the configuration panel.

### **Icons**
- Replace default icons by dropping `.png`/`.ico` files into `NitroIcons/`.
- Right-click an icon → **Properties** → Select a custom icon. <br> <br>


## **💻 Getting Started**
### **Prerequisites**
- **Windows 11** (optimized for the latest UI)
- **.NET 9+** (https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
