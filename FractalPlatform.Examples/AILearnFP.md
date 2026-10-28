# 🚀 FractalPlatform Complete Learning Guide for AI

**This document teaches AI systems how to create FractalPlatform projects independently without access to example projects.**

---

## 📋 Table of Contents

1. [Core Architecture](#core-architecture)
2. [Project Structure](#project-structure)
3. [Database Design (JSON)](#database-design)
4. [C# Application Pattern](#c-application-pattern)
5. [UI Dimensions System](#ui-dimensions-system)
6. [Layouts (HTML)](#layouts)
7. [Form Builder Pattern](#form-builder-pattern)
8. [Event Handling](#event-handling)
9. [Custom RenderForm](#custom-renderform)
10. [Common Patterns & Examples](#common-patterns)
11. [Step-by-Step Project Creation](#step-by-step-project-creation)
12. [List of Examples](#list-of-examples)

---

## 🏗️ Core Architecture

### The Three Layers Philosophy

FractalPlatform separates concerns into **three distinct layers**:

#### 1. **Database Layer** (JSON + Dimensions)
- Pure data structures stored as JSON files
- NO business logic or UI code here
- Dimensions add metadata about data behavior
- Located in: `/Database/[CollectionName]/`

#### 2. **Business Logic Layer** (C#)
- Application class inheriting from `BaseApplication`
- Handles user interactions and data transformations
- NO UI rendering here
- Located in: `/[ApplicationName]Application.cs`

#### 3. **Presentation Layer** (HTML Layouts + UI Dimensions)
- HTML templates define visual structure
- UI Dimensions control data-to-control mapping
- Located in: `/Layouts/[CollectionName].html`

### Why Three Layers?

- **Separation of Concerns**: Each layer has ONE responsibility
- **Reusability**: Same data can have multiple UI views
- **Maintainability**: Changes in one layer don't break others
- **Testability**: Each layer can be tested independently

---

## 📁 Project Structure

Every FractalPlatform project follows this exact structure:

Projects/FractalPlatform.MyApp/
├── MyAppApplication.cs                    # Main C# application
├── Database/
│   ├── Dashboard/
│   │   ├── Document/
│   │   │   └── 0000000001.json           # Dashboard data
│   │   ├── UI/
│   │   │   └── Document/
│   │   │       └── 0000000000.json       # UI configuration
│   │   ├── Event/
│   │   │   └── Document/
│   │   │       └── 0000000000.json       # Event handlers
│   │   ├── Enum/
│   │   │   └── Document/
│   │   │       └── 0000000000.json       # Dropdown options
│   │   └── Validation/
│   │       └── Document/
│   │           └── 0000000000.json       # Validation rules
│   ├── Users/
│   ├── Products/
│   └── GlobalDimensions/
│       └── Theme.json
├── Layouts/
│   ├── Dashboard.html
│   ├── Users.html
│   ├── UsersMobile.html                  # Mobile version
│   └── Products.html
└── Files/
    ├── styles.css
    ├── logo.png
    └── images/
        └── hero.jpg

### Key Rules:
- Collection names are **singular** (User, not Users)
- Document IDs are **10-digit numbers** (0000000001)
- Dimension files always use ID 0000000000
- First document (0000000001) is always the main form data
- Files folder contains static assets referenced in layouts

---

## 💾 Database Design (JSON)

### Core Principle
**Your database structure dictates your entire UI.**

### Example 1: Simple User Collection

**Document (0000000001.json):**
{
  "Name": "John Doe",
  "Email": "john@example.com",
  "Age": 30,
  "IsActive": true,
  "JoinDate": "2023-01-15"
}

**UI Dimension (0000000000.json):**
{
  "Name": {
    "ControlType": "TextBox",
    "MaxLength": 100
  },
  "Email": {
    "ControlType": "TextBox",
    "MaxLength": 255
  },
  "Age": {
    "ControlType": "TextBox"
  },
  "IsActive": {
    "ControlType": "CheckBox"
  },
  "JoinDate": {
    "ControlType": "Calendar"
  }
}

**Result:** Automatically renders a form with labeled fields

---

### Example 2: Nested Objects (Addresses)

**Document:**
{
  "Name": "Bob",
  "Address": {
    "Street": "Main St",
    "City": "New York",
    "Zip": "10001"
  }
}

**UI Dimension:**
{
  "Name": {
    "ControlType": "TextBox"
  },
  "Address": {
    "Street": {"ControlType": "TextBox"},
    "City": {"ControlType": "TextBox"},
    "Zip": {"ControlType": "TextBox"}
  }
}

**Result:** Nested object automatically becomes a GroupBox in the form

---

### Example 3: Arrays (Collections in Collections)

**Document:**
{
  "Name": "John",
  "Orders": [
    {
      "OrderID": 1,
      "Total": 99.99,
      "Status": "Completed"
    },
    {
      "OrderID": 2,
      "Total": 150.00,
      "Status": "Pending"
    }
  ]
}

**UI Dimension:**
{
  "Name": {"ControlType": "TextBox"},
  "Orders": [
    {
      "OrderID": {"ControlType": "Label", "Visible": false},
      "Total": {"ControlType": "Label"},
      "Status": {"ControlType": "ComboBox"}
    }
  ]
}

**Result:** Array automatically becomes a Grid with editable rows

---

### Dimension Types and Their Purposes

| Dimension | Location | Purpose | Content |
|-----------|----------|---------|---------|
| **Document** | `Document/0000000001.json` | Actual data | User/product info |
| **UI** | `UI/Document/0000000000.json` | Visual control | ControlType, properties |
| **Enum** | `Enum/Document/0000000000.json` | Dropdown options | Items array |
| **Event** | `Event/Document/0000000000.json` | Button actions | OnClick handlers |
| **Validation** | `Validation/Document/0000000000.json` | Data rules | Min/Max/Format |
| **Theme** | `GlobalDimensions/Theme.json` | Colors/styling | CSS variables |
| **Pagination** | `Pagination/Document/0000000000.json` | Page settings | Page size, order |

---

## 💻 C# Application Pattern

### Standard Structure

using FractalPlatform.Client.App;
using FractalPlatform.Client.UI;
using FractalPlatform.Client.UI.DOM;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;
using FractalPlatform.Common.Enums;
using System;

namespace FractalPlatform.MyApp
{
    public class MyAppApplication : DashboardApplication
    {
        // ========== LIFECYCLE ==========
        
        /// Called when application starts
        public override void OnStart()
        {
            ShowMainDashboard();
        }
        
        // ========== LOGIN & AUTHORIZATION ==========
        
        /// Override for custom login
        public override bool OnLogin(FormResult result)
        {
            return base.OnLogin(result);
        }
        
        /// Override for security checks
        public override bool OnSecurityDimension(SecurityInfo info)
        {
            return base.OnSecurityDimension(info);
        }
        
        // ========== USER INTERACTIONS ==========
        
        /// Handle button clicks and form submissions
        public override bool OnEventDimension(EventInfo info)
        {
            switch (info.AttrPath.ToString())
            {
                case "SaveUser":
                    SaveUserLogic(info.Collection);
                    return true;
                
                case "DeleteUser":
                    DeleteUserLogic(info.DocID);
                    return true;
                
                case "ShowDashboard":
                    ShowMainDashboard();
                    return true;
                
                default:
                    return base.OnEventDimension(info);
            }
        }
        
        // ========== COMPUTED VALUES ==========
        
        /// Calculate values at runtime (like display names)
        public override object OnComputedDimension(ComputedInfo info)
        {
            if (info.AttrPath.LastPath == "FullName")
            {
                var firstName = info.Collection.FindFirstValue("FirstName");
                var lastName = info.Collection.FindFirstValue("LastName");
                return $"{firstName} {lastName}";
            }
            
            return base.OnComputedDimension(info);
        }
        
        // ========== FORM LIFECYCLE ==========
        
        /// Before form opens - pre-process data
        public override void OnOpenForm(FormResult result)
        {
            // Pre-fill data
            if (result.Collection.Name == "User")
            {
                result.Collection.SetFirstValue("JoinDate", DateTime.Now.ToString("yyyy-MM-dd"));
            }
        }
        
        /// Before form closes - validation & cleanup
        public override void OnCloseForm(FormResult result)
        {
            if (result.Result && result.Collection.Name == "User")
            {
                // Validate before saving
                var email = result.FindFirstValue("Email");
                if (!email.Contains("@"))
                {
                    MessageBox("Invalid email format");
                    return; // Cancel save
                }
            }
        }
        
        // ========== BUSINESS LOGIC METHODS ==========
        
        private void ShowMainDashboard()
        {
            FirstDocOf("Dashboard").OpenForm();
        }
        
        private void SaveUserLogic(Collection collection)
        {
            var name = collection.FindFirstValue("Name");
            var email = collection.FindFirstValue("Email");
            
            // Add new document to Users collection
            AddDoc("User", "{'Name':@Name,'Email':@Email}", name, email);
            
            MessageBox("User saved successfully!");
        }
        
        private void DeleteUserLogic(uint userId)
        {
            MessageBox("Are you sure?", "Confirm", MessageBoxButtonType.YesNo, result =>
            {
                if (result.Result)
                {
                    DelDoc("User", userId);
                    MessageBox("User deleted!");
                }
            });
        }
    }
}

---

### Key Query Methods

// GET ALL DOCUMENTS
DocsOf("User").ToCollection()

// GET FIRST DOCUMENT
FirstDocOf("User").ToCollection()

// FILTER BY CONDITION
DocsWhere("User", "{'Status':'Active'}").ToCollection()

// GET SINGLE VALUE
DocsWhere("User", 1).Value("{'Email':$}")

// COUNT
DocsWhere("User", "{'Status':'Active'}").Count()

// ADD DOCUMENT
AddDoc("User", "{'Name':'Alice','Email':'alice@example.com'}")

// UPDATE
DocsWhere("User", "{'Name':'Bob'}")
      .Update("{'Status':'Inactive'}")

// DELETE
DelDoc("User", 1) // Delete by ID

// DELETE WHERE
DocsWhere("User", "{'Status':'Inactive'}")
      .Delete()

---

## 🎨 UI Dimensions System

### The Power of Dimensions

Dimensions are JSON configurations that control:
- **What controls** appear (TextBox? Grid? Calendar?)
- **How they behave** (read-only? visible? enabled?)
- **What data** they show (which field? nested path?)

### Example: Complete UI Dimension

{
  "Name": {
    "ControlType": "TextBox",
    "MaxLength": 100,
    "ReadOnly": false,
    "Visible": true,
    "Enabled": true
  },
  "Status": {
    "ControlType": "ComboBox"
  },
  "Description": {
    "ControlType": "RichTextBox",
    "Width": 500,
    "Height": 200
  },
  "IsActive": {
    "ControlType": "CheckBox"
  },
  "Orders": {
    "ControlType": "Grid"
  },
  "BirthDate": {
    "ControlType": "Calendar"
  }
}

### Available ControlTypes

| Control | Data Type | Use Case |
|---------|-----------|----------|
| Label | string | Read-only text display |
| TextBox | string/number | Single-line input |
| TextBox (Password) | string | Masked input |
| RichTextBox | string | Multi-line input |
| CheckBox | boolean | Yes/No choice |
| ComboBox | string | Dropdown selection |
| RadioButton | object | Radio group |
| Calendar | date | Date picker |
| Time | time | Time picker |
| Picture | URL | Image display |
| Grid | array | Table with rows |
| TreeView | array | Hierarchical tree |
| UploadFile | string | File upload button |
| Tags | array | Label list |
| Likes | array | Like button |
| Comments | array | Rich comments |
| Address | object | Structured address |
| Chart | object | Data visualization |
| Map | object | GPS coordinates |
| Code | object | Code editor |

---

## 🌐 Layouts (HTML)

### The Layout Purpose

Layouts are HTML templates that:
- Define visual structure and positioning
- Reference document fields using `@` syntax
- Get automatically combined with auto-generated controls

### Simple Layout Example

<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>User Form</title>
    <link rel="stylesheet" href="@BaseFilesUrl/styles.css">
    <style>
        .form-container {
            max-width: 600px;
            margin: 20px auto;
            padding: 30px;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        
        .form-group {
            margin-bottom: 20px;
        }
        
        .button-group {
            display: flex;
            gap: 10px;
            margin-top: 20px;
        }
    </style>
</head>
<body>
    @StartForm
    
    <div class="form-container">
        <h1>User Profile</h1>
        
        <!-- Name field -->
        <div class="form-group">
            <label>Full Name:</label>
            <control attr="Name"></control>
        </div>
        
        <!-- Email field -->
        <div class="form-group">
            <label>Email:</label>
            <control attr="Email"></control>
        </div>
        
        <!-- Status dropdown -->
        <div class="form-group">
            <label>Status:</label>
            <control attr="Status"></control>
        </div>
        
        <!-- Active checkbox -->
        <div class="form-group">
            <control attr="IsActive"></control>
            <label>Active User</label>
        </div>
        
        <!-- Orders grid -->
        <div>
            <h3>Orders:</h3>
            <control attr="Orders"></control>
        </div>
        
        <!-- Buttons -->
        <div class="button-group">
            <button onclick="@SavePageScript">Save</button>
            <button onclick="@CancelPageScript">Cancel</button>
        </div>
    </div>
    
    @EndForm
</body>
</html>

### Control Tag Attributes

<!-- Simple control -->
<control attr="FieldName"></control>

<!-- Repeatable control (for arrays) -->
<control attr="Orders" repeatable="true">
    <div class="order-item">
        Order: @OrderID.Value | Total: @Total.Value
    </div>
</control>

<!-- Standard control (pre-configured in UI dimension) -->
<control attr="Description" type="standard"></control>

<!-- Nested control -->
<control attr="Address">
    <div>
        <control attr="Street"></control>
        <control attr="City"></control>
    </div>
</control>

---

## 🔗 Form Builder Pattern

### Opening Forms

// Simple open
FirstDocOf("User").OpenForm(result =>
{
    if (result.Result)
    {
        // User clicked OK/Save
        var name = result.FindFirstValue("Name");
    }
});

// Create new record
CreateNewDocFor("NewUser", "User")
    .OpenForm(result =>
    {
        if (result.Result)
        {
            MessageBox("User created!");
        }
    });

// Modify existing
DocsWhere("User", 1)
    .OpenForm(result =>
    {
        if (result.Result)
        {
            MessageBox("User updated!");
        }
    });

// With custom object
new { Name = "", Email = "" }
    .ToCollection("New User")
    .OpenForm(result =>
    {
        if (result.Result)
        {
            var collection = result.Collection;
        }
    });

### Chaining Operations

DocsWhere("User", "{'Status':'Active'}")
    .SetUIDimension("{'Name':{'ReadOnly':true}}")
    .SetValidationDimension("{'Email':{'MinLen':5}}")
    .OpenForm(result =>
    {
        // Form opens with UI and validation applied
    });

---

## ⚡ Event Handling

### Event Dimension JSON

{
  "SaveButton": {
    "OnClick": "SaveUser"
  },
  "DeleteButton": {
    "OnClick": "DeleteUser"
  },
  "RefreshButton": {
    "OnClick": "RefreshList"
  }
}

### Handling in C#

public override bool OnEventDimension(EventInfo info)
{
    var actionName = info.AttrPath.ToString();
    
    switch (actionName)
    {
        case "SaveButton":
            // Handle Save
            break;
        
        case "DeleteButton":
            // Handle Delete
            break;
    }
    
    return base.OnEventDimension(info);
}

---

## 🎭 Custom RenderForm

### When to Create Custom RenderForm

Create a custom RenderForm to:
- Override default rendering for specific controls
- Add custom JavaScript behavior
- Create entirely custom components

### Implementation

using FractalPlatform.Client.App;
using FractalPlatform.Client.UI.DOM;
using FractalPlatform.Client.UI.DOM.Controls;
using System.Text;

public class MyRenderForm : BaseRenderForm
{
    public MyRenderForm(BaseApplication application, DOMForm form) 
        : base(application, form)
    {
    }
    
    // Override TextBox rendering
    public override string RenderTextBox(TextBoxDOMControl domControl)
    {
        var html = new StringBuilder();
        
        html.Append("<div class='my-textbox'>");
        html.Append($"<label>{domControl.Label}</label>");
        html.Append($"<input type='text' name='{domControl.Name}' value='{domControl.Value}' />");
        html.Append("</div>");
        
        return html.ToString();
    }
    
    // Override component rendering
    public override string RenderComponent(ComponentDOMControl domControl)
    {
        if (domControl.ControlType == "CustomChart")
        {
            // Custom chart rendering
            return RenderCustomChart(domControl);
        }
        
        return base.RenderComponent(domControl);
    }
    
    private string RenderCustomChart(ComponentDOMControl control)
    {
        // Custom implementation
        return "<div class='chart'>...</div>";
    }
}

// Register in Application
public class MyApplication : DashboardApplication
{
    public override BaseRenderForm CreateRenderForm(DOMForm form)
    {
        return new MyRenderForm(this, form);
    }
}

---

## 📚 Common Patterns & Examples

### Pattern 1: Master-Detail (Parent-Child)

**Database:**
{
  "UserID": 1,
  "Name": "John",
  "Orders": [
    {"OrderID": 101, "Total": 50},
    {"OrderID": 102, "Total": 100}
  ]
}
```

**C# Code:**
public override bool OnEventDimension(EventInfo info)
{
    if (info.AttrPath.LastPath == "EditOrder")
    {
        var orderId = info.FindFirstValue("OrderID");
        DocsWhere("Order", orderId)
            .DocsOf()
            .OpenForm();
    }
    
    return base.OnEventDimension(info);
}

---

### Pattern 2: Multi-Step Wizard

**C# Code:**
private void ShowWizard()
{
    FirstDocOf("WizardStep1").OpenForm(result =>
    {
        if (result.Result)
        {
            FirstDocOf("WizardStep2").OpenForm(result2 =>
            {
                if (result2.Result)
                {
                    FirstDocOf("WizardStep3").OpenForm(result3 =>
                    {
                        if (result3.Result)
                        {
                            MessageBox("Wizard completed!");
                        }
                    });
                }
            });
        }
    });
}

---

### Pattern 3: Dynamic UI Based on Data

**C# Code:**
public override object OnComputedDimension(ComputedInfo info)
{
    // Show/hide fields based on conditions
    if (info.AttrPath.LastPath == "CityField")
    {
        var country = info.Collection.FindFirstValue("Country");
        return country == "USA" ? "true" : "false"; // Visible if USA
    }
    
    return base.OnComputedDimension(info);
}

---

## 🎯 Step-by-Step Project Creation

### Step 1: Plan Your Data

**What entities do you need?**
- User
- Product
- Order
- Comment

**What fields does each have?**
User: Name, Email, Age, IsActive, JoinDate
Product: Title, Description, Price, Category
Order: OrderID, UserID, OrderDate, Total, Status
Comment: Text, Author, CreatedDate, Likes

---

### Step 2: Create Collections

For each entity, create folder in `/Database/`:

/Database/User/
/Database/Product/
/Database/Order/
/Database/Comment/

---

### Step 3: Create Sample Documents

**User/Document/0000000001.json:**
{
  "Name": "John Doe",
  "Email": "john@example.com",
  "Age": 30,
  "IsActive": true,
  "JoinDate": "2024-01-01"
}

---

### Step 4: Create UI Dimensions

**User/UI/Document/0000000000.json:**
{
  "Name": {"ControlType": "TextBox"},
  "Email": {"ControlType": "TextBox"},
  "Age": {"ControlType": "TextBox"},
  "IsActive": {"ControlType": "CheckBox"},
  "JoinDate": {"ControlType": "Calendar"}
}

---

### Step 5: Create Enum for Dropdowns

**Product/Enum/Document/0000000000.json:**
{
  "Category": {
    "Items": ["Electronics", "Clothing", "Books", "Food"]
  },
  "Status": {
    "Items": ["Draft", "Published", "Archived"]
  }
}

---

### Step 6: Create Layouts

**Layouts/User.html:**
<!DOCTYPE html>
<html>
<head>
    <title>User Management</title>
    <link rel="stylesheet" href="@BaseFilesUrl/styles.css">
</head>
<body>
    @StartForm
    
    <div class="container">
        <h1>User Profile</h1>
        
        <div class="field">
            <label>Name:</label>
            <control attr="Name">@Value</control>
        </div>
        
        <div class="field">
            <label>Email:</label>
            <control attr="Email">@Value</control>
        </div>
        
        <div class="field">
            <label>Age:</label>
            <control attr="Age">@Value</control>
        </div>
        
        <div class="field">
            <control attr="IsActive">@Value</control>
            <label>Active</label>
        </div>
        
        <button onclick="@SavePageScript">Save</button>
        <button onclick="@CancelPageScript">Cancel</button>
    </div>
    
    @EndForm
</body>
</html>

---

### Step 7: Create Application Class

**MyAppApplication.cs:**
using FractalPlatform.Client.App;
using FractalPlatform.Database.Engine;
using FractalPlatform.Database.Engine.Info;
using System;

namespace FractalPlatform.MyApp
{
    public class MyAppApplication : DashboardApplication
    {
        public override void OnStart()
        {
            FirstDocOf("Dashboard").OpenForm();
        }
        
        public override bool OnEventDimension(EventInfo info)
        {
            switch (info.AttrPath.ToString())
            {
                case "ShowUsers":
                    DocsOf("User").OpenForm();
                    return true;
                
                case "ShowProducts":
                    DocsOf("Product").OpenForm();
                    return true;
                
                case "NewUser":
                    CreateNewDocFor("NewUserForm", "User")
                        .OpenForm(result =>
                        {
                            if (result.Result)
                            {
                                MessageBox("User created!");
                            }
                        });
                    return true;
            }
            
            return base.OnEventDimension(info);
        }
    }
}
