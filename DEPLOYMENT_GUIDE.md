# 🚀 AI Resume Analyzer - Cloud Deployment Guide

This guide provides step-by-step instructions to deploy the **Python FastAPI Backend** on **Render.com** and the **ASP.NET Core 8 MVC Frontend** on either **Render.com** (recommended for a single-platform setup) or **Microsoft Azure App Service**.

---

## 📋 Prerequisites

Before starting, ensure you have:
1. A **GitHub Account** with your code pushed to `https://github.com/saravanarms/RESUME-ATS`.
2. A **Render.com Account** (Free tier is sufficient).
3. (Optional) A **Microsoft Azure Account** (if choosing the Azure deployment path).

---

## 🛠️ Phase 1: Deploy Backend to Render.com

The backend is built in Python (FastAPI) and packaged using Docker. Render.com is ideal for running Dockerized web services.

### Step 1: Connect GitHub to Render
1. Log in to your [Render Dashboard](https://dashboard.render.com/).
2. Click **New +** in the top-right and select **Web Service**.
3. Choose **Build and deploy from a Git repository**.
4. Connect your GitHub account and select the `RESUME-ATS` repository.

### Step 2: Configure Web Service Settings
On the creation screen, configure the following values:
* **Name:** `resume-analyzer-ai-service`
* **Region:** Choose a region close to you (e.g., `Singapore` or `Oregon`).
* **Branch:** `main`
* **Runtime:** `Docker`
* **Docker Context Path:** `./src/ResumeAnalyzer.Backend`
* **Dockerfile Path:** `./src/ResumeAnalyzer.Backend/Dockerfile`
* **Instance Type:** `Free` (or any paid tier)

### Step 3: Add Environment Variables
Scroll down to the **Environment Variables** section and add:
1. `JWT_SECRET`: Generate a secure random string (e.g., a 64-character hex string). *Keep this handy, as you will need to add it to your frontend too.*
2. `RATE_LIMIT_REQUESTS`: `100` (default)
3. `RATE_LIMIT_WINDOW_SECONDS`: `60` (default)

### Step 4: Create Service & Retrieve Deploy Webhook
1. Click **Create Web Service**. Render will start pulling your code and building the Docker container.
2. Once creation starts, look at the **Deploy Hook** section on the service details page.
3. Copy the **Deploy Hook URL** (it looks like `https://api.render.com/deploy/srv-...`). You will use this in your GitHub secrets.
4. Copy your service's live URL (e.g., `https://resume-analyzer-ai-service.onrender.com`). You will need this for the frontend configuration.

---

## ☁️ Option A: Deploy Frontend to Render.com (Recommended)

You can host both the frontend and backend on Render using Docker. Since Render supports multi-stage Docker builds, it will compile and run the ASP.NET Core application automatically.

### Step 1: Create a New Web Service for the Frontend
1. In your [Render Dashboard](https://dashboard.render.com/), click **New +** and select **Web Service**.
2. Choose **Build and deploy from a Git repository**.
3. Select your `RESUME-ATS` repository.

### Step 2: Configure Frontend Service Settings
Configure the following fields:
* **Name:** `resume-analyzer-web-app`
* **Region:** Match the region of your backend web service (e.g., `Singapore` or `Oregon`).
* **Branch:** `main`
* **Runtime:** `Docker`
* **Root Directory:** *Leave this completely blank/empty* (this ensures Render can access the root `.sln` file and sibling projects for building)
* **Dockerfile Path:** `src/ResumeAnalyzer.Web/Dockerfile`
* **Instance Type:** `Free` (or any paid tier)

### Step 3: Add Environment Variables
Scroll down to the **Environment Variables** section and add:
1. `BackendService__BaseUrl`: Your Render backend live URL (from Phase 1, e.g., `https://resume-analyzer-ai-service.onrender.com`).
2. `BackendService__JwtSecret`: The exact same JWT Secret string used in the backend configuration.
3. `ASPNETCORE_ENVIRONMENT`: `Production`

> [!NOTE]
> This setup uses a local SQLite database (`ResumeAnalyzer.db`) inside the running Docker container, meaning registered accounts and uploaded resumes will reset whenever Render restarts the container (due to inactivity or daily updates).
> For demo and testing purposes, this is perfect. If you want permanent database storage, you can create a **PostgreSQL database** on Render and add the connection string under `ConnectionStrings__DefaultConnection` in the environment variables.

---

## ☁️ Option B: Deploy Frontend to Microsoft Azure

The frontend is an ASP.NET Core 8 MVC application. We will host it on **Azure App Service (Web App)**.

### Step 1: Create an Azure Web App
1. Log in to the [Azure Portal](https://portal.azure.com/).
2. Click **Create a resource** and search for **Web App**. Click **Create**.
3. Configure the following fields:
   - **Subscription:** Select your active subscription.
   - **Resource Group:** Click *Create new* (e.g., `rg-resume-analyzer`).
   - **Name:** `resume-analyzer-web-app` *(This must be unique. If it's taken, choose another name and remember to update it in your `.github/workflows/frontend-deploy.yml` file)*.
   - **Publish:** `Code`
   - **Runtime Stack:** `.NET 8 (LTS)`
   - **Operating System:** `Linux` (Recommended and cost-effective)
   - **Region:** Match the region of your Render backend.
   - **Pricing Plan:** Select the **Free F1** tier (or **Basic B1** if you want faster performance).
4. Click **Review + Create**, then click **Create**. Wait for deployment to complete.

### Step 2: Create Azure Blob Storage (Optional but Recommended)
If you don't configure Azure Storage, uploads will save to the local file system. However, App Service instances recycle frequently, meaning uploaded files will be lost unless persistent Blob Storage is configured.
1. In the Azure Portal, search for **Storage accounts** and click **Create**.
2. Set the Resource Group to `rg-resume-analyzer` and give it a unique name (e.g., `stresumeanalyzer`).
3. Click **Review + Create** and then **Create**.
4. Once created, go to the storage account, select **Access keys** under Security + networking, and copy the **Connection string** (Key 1).
5. Go to **Containers** under Data storage, click **+ Container**, and name it `resumes` with **Blob (anonymous read access for blobs)** access level.

### Step 3: Configure App Service Settings
Go to your newly created Azure Web App page:
1. Select **Environment variables** (under *Settings* in the left menu).
2. Under **App settings**, add the following variables:
   - `BackendService__BaseUrl`: Your Render backend live URL (e.g., `https://resume-analyzer-ai-service.onrender.com`).
   - `BackendService__JwtSecret`: The exact same secure string used as `JWT_SECRET` in Render.
3. Under the **Connection strings** tab:
   - Add a new connection string named `AzureBlobStorage`.
   - **Value:** Paste the storage account Connection String from Step 2.
   - **Type:** Select `Custom` or `MySql` (leave as Custom).
4. Click **Apply** at the bottom to save all configurations.

### Step 4: Retrieve the Publish Profile
1. On your Azure Web App overview page, click **Get publish profile** from the top toolbar.
2. A `.PublishSettings` file will download to your local machine. Open this file in a text editor (like Notepad) and copy the entire XML content.

---

## 🔄 Phase 3: Connect CI/CD Workflows via GitHub Secrets

Now we need to tell GitHub how to deploy automatically whenever you push code changes to your repository.

### Step 1: Configure Secrets in GitHub
1. Go to your repository on GitHub: `https://github.com/saravanarms/RESUME-ATS`.
2. Click **Settings** -> **Secrets and variables** -> **Actions**.
3. Click **New repository secret** and add the following two secrets:

| Secret Name | Value |
| :--- | :--- |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Paste the entire XML content of the `.PublishSettings` file you downloaded from Azure in Phase 2, Step 4. |
| `RENDER_DEPLOY_HOOK_URL` | Paste the Render deploy hook URL you copied in Phase 1, Step 4. |

### Step 2: Update Web App Name in Workflow (if changed)
If you had to choose a different name for your Azure Web App because `resume-analyzer-web-app` was taken:
1. In your local repository or on GitHub editor, open [.github/workflows/frontend-deploy.yml](file:///e:/RMS/Documents/Downloads/Resume%20Analyser%20Website/.github/workflows/frontend-deploy.yml).
2. Change the `app-name` on line 38 to match your actual Azure Web App name:
   ```yaml
   app-name: 'your-actual-azure-web-app-name'
   ```
3. Commit and push this change to GitHub.

---

## 🚀 Step 4: Run the Deployments

Now that secrets are configured, any push to the `main` branch will automatically build and deploy the application:

1. **Trigger Deployments:**
   Make a minor change or manually trigger the GitHub Actions workflows.
   - Go to your GitHub repository -> **Actions** tab.
   - Select **Deploy Frontend to Azure App Service** or **Deploy Backend to Render.com**.
   - Click **Run workflow** -> **Run workflow**.

2. **Monitor Actions:**
   Watch the workflows run. The backend workflow will send a curl request triggering Render to build, and the frontend workflow will compile the .NET app and push it directly to Azure App Service.

3. **Verify App Deployment:**
   - Once completed, visit your Azure Web App URL (e.g., `https://resume-analyzer-web-app.azurewebsites.net`).
   - Register an account, upload a PDF resume, paste a job description, and run an analysis to verify the frontend successfully routes requests to the backend!
