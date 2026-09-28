# How to View the Gemora Shipping UI

## Quick Start - Demo Mode (No Backend Required)

### Step 1: Open Your Browser
Navigate to: **http://localhost:5174**

### Step 2: Click "View Demo" Button
On the login page, you'll see a new section that says:
> "Want to see the UI without a backend?"

Click the purple **"View Demo (Admin Dashboard)"** button.

### Step 3: Explore the Dashboard
You'll be taken directly to the Admin Shipping Dashboard with **5 mock shipments** showing:

- **Statistics Cards**: Total, Pending, In Transit, Delivered, Exceptions
- **Shipment Table**: Filterable by status (All, Pending, In Transit, etc.)
- **Color-coded Status Badges**: Green (Delivered), Yellow (Pending/In Transit), Red (Exceptions)

### Step 4: View Shipment Details
Click **"View Details"** on any shipment to see:

#### Shipment Information
- Shipment number, order ID, status
- Origin → Destination
- Declared value with currency
- Package description
- Service type and courier
- Tracking number

#### Shipping Plan Section
- Risk level indicator (Low/Medium/High)
- Insurance recommendation
- Coverage amount
- Requirements and warnings
- **"Approve Plan"** button (for pending plans)

#### Tracking Timeline
- Visual timeline with markers
- Event timestamps
- Location information
- Status descriptions

#### Insurance Information
- Provider details
- Policy reference
- Coverage amount and type
- Premium cost

---

## Full Mode - With Backend Running

### Terminal 1: Start Backend
```bash
cd "C:\Users\levin\Desktop\SLIIT\3rd 1\SE3090 – Software Engineering Frameworks\PROJECT\Component 3 - Secure Shipping and Insurance\GemoraMarketplace\Backend\Gemora.API"
dotnet run
```

Backend will start on: **http://localhost:5198**

### Terminal 2: Frontend (Already Running)
The React dev server is already running on: **http://localhost:5174**

### Step 1: Register an Admin Account
1. Go to http://localhost:5174/register
2. Fill in the form
3. Select role: **Admin**
4. Create account

### Step 2: Login
1. Go to http://localhost:5174/login
2. Enter your admin credentials
3. Click "Sign In"

### Step 3: View Real Data
You'll be redirected to `/admin` where you can see:
- Real shipments from the database
- Actual tracking events
- Real insurance records
- Working approval workflow

---

## What You Can Test in Demo Mode

✅ **Dashboard Statistics** - See how counts are calculated  
✅ **Status Filtering** - Filter shipments by status dropdown  
✅ **Shipment Details Page** - Full detail view with all sections  
✅ **Approval Workflow UI** - See the approve button and confirmation dialog  
✅ **Tracking Timeline** - Visual timeline display (mock data)  
✅ **Insurance Cards** - Insurance information display (mock data)  
✅ **Responsive Design** - Works on different screen sizes  

---

## Files Modified for Demo Mode

1. **Login.jsx** - Added "View Demo" button that sets mock auth token
2. **AdminDashboard.jsx** - Added mock data loader and demo mode indicator

These changes don't affect production functionality - they just make it easier to preview the UI without needing a full backend setup.

---

## Next Steps

Once you're satisfied with the UI:

1. **Start the backend** to test real API integration
2. **Create test shipments** via the API or database
3. **Test the full workflow**: Create → Generate Plan → Approve → Track
4. **Try different user roles** (Seller, Buyer) to see role-based access

---

**Frontend is running at:** http://localhost:5174  
**Backend should run at:** http://localhost:5198 (when started)
