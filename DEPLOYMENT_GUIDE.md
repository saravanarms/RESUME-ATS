# 🚀 AI Resume Analyzer - Cloud Deployment Guide

This guide provides step-by-step instructions to deploy the **Python FastAPI Backend** on **Render.com** and the **Static HTML/CSS/JS Frontend** on either **Render.com** or **Microsoft Azure Static Web Apps** (Free Tier).

---

## 📋 Prerequisites

Before starting, ensure you have:
1. A **GitHub Account** with your code pushed to `https://github.com/saravanarms/RESUME-ATS`.
2. A **Render.com Account** (Free tier is sufficient).
3. A **Microsoft Azure Account** (Free tier is sufficient for Static Web Apps).

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
1. `JWT_SECRET`: Generate a secure random string (e.g., a 64-character hex string). *This is used by the backend security module.*
2. `RATE_LIMIT_REQUESTS`: `100` (default)
3. `RATE_LIMIT_WINDOW_SECONDS`: `60` (default)

### Step 4: Create Service & Retrieve Live URL
1. Click **Create Web Service**. Render will start pulling your code and building the Docker container.
2. Copy your service's live URL once created (e.g., `https://resume-analyzer-ai-service.onrender.com`). You will need this for the frontend configuration.

---

## ☁️ Option A: Deploy Frontend to Render.com (Alternative)

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

---

## ☁️ Option B: Deploy Frontend to Microsoft Azure Static Web Apps (Recommended)

Azure Static Web Apps provides free hosting for static client-side applications (HTML/CSS/JS).

### Step 1: Create an Azure Static Web App
1. Log in to the [Azure Portal](https://portal.azure.com/).
2. Search for **Static Web Apps** and click **Create**.
3. Configure the following fields:
   - **Subscription:** Select your active subscription.
   - **Resource Group:** Click *Create new* (e.g., `rg-resume-analyzer`).
   - **Name:** `resume-analyzer-static`
   - **Plan type:** `Free`
   - **Source:** Choose **GitHub**.
   - Sign in to GitHub and select:
     - **Organization:** Your username.
     - **Repository:** `RESUME-ATS`.
     - **Branch:** `main`.
4. In the **Build Details** section:
   - **Build Presets:** Select `HTML`.
   - **App location:** `/src/ResumeAnalyzer.Static`
   - **Api location:** *Leave blank*
   - **Output location:** *Leave blank*
5. Click **Review + Create**, then click **Create**.

### Step 2: Retrieve the Deployment Token
1. Once the Static Web App is deployed, navigate to its overview page in the Azure portal.
2. Click **Manage deployment token** in the top menu bar.
3. Copy the token string.

### Step 3: Add the Token to GitHub Actions Secrets
1. Go to your repository on GitHub: `https://github.com/saravanarms/RESUME-ATS`.
2. Click **Settings** -> **Secrets and variables** -> **Actions**.
3. Click **New repository secret** and add a secret named:
   - **Name:** `AZURE_STATIC_WEB_APPS_API_TOKEN`
   - **Value:** Paste the deployment token you copied in Step 2.

---

## 🚀 Step 4: Run the Deployments

Any push to the `main` branch will now automatically build and deploy the application:

1. **Trigger Deployments:**
   Make a minor change or manually trigger the GitHub Actions workflows.
   - Go to your GitHub repository -> **Actions** tab.
   - Select **Deploy Frontend to Azure Static Web Apps**.
   - Click **Run workflow** -> **Run workflow**.

2. **Verify App Deployment:**
   - Once completed, visit your Azure Static Web App URL (visible in your Azure overview page).
   - Drag and drop a resume, add a job description, and click analyze to verify the frontend successfully routes requests to the public Render backend API!
