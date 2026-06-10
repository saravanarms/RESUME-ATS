const API_BASE_URL = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1' 
    ? 'http://127.0.0.1:8000' 
    : 'https://resume-ats-p6eb.onrender.com'; // Replace with your production Render URL if different

let selectedFile = null;

// Initialize Page
document.addEventListener("DOMContentLoaded", () => {
    initTheme();
    initUploadZone();
    loadHistory();
    
    // Hook up buttons
    document.getElementById("btn-analyze").addEventListener("click", handleAnalyze);
    document.getElementById("btn-new-upload").addEventListener("click", resetToUploadView);
    document.getElementById("theme-toggle").addEventListener("click", toggleTheme);
    
    // Default general job description placeholder for quick test
    document.getElementById("job-description").placeholder = 
`e.g., We are looking for a Software Engineer with experience in Python, Web APIs (FastAPI/Django), Docker, and databases like PostgreSQL. Strong communication, teamwork, and problem-solving skills are required.`;
});

// Theme Management (Light / Dark)
function initTheme() {
    const savedTheme = localStorage.getItem("theme") || "light";
    document.documentElement.setAttribute("data-theme", savedTheme);
    updateThemeIcon(savedTheme);
}

function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute("data-theme");
    const newTheme = currentTheme === "dark" ? "light" : "dark";
    document.documentElement.setAttribute("data-theme", newTheme);
    localStorage.setItem("theme", newTheme);
    updateThemeIcon(newTheme);
}

function updateThemeIcon(theme) {
    const icon = document.getElementById("theme-icon");
    if (theme === "dark") {
        icon.className = "bi bi-sun-fill";
    } else {
        icon.className = "bi bi-moon-fill";
    }
}

// Drag & Drop Upload Zone
function initUploadZone() {
    const zone = document.getElementById("upload-zone");
    const fileInput = document.getElementById("file-input");

    zone.addEventListener("click", () => fileInput.click());
    
    fileInput.addEventListener("change", (e) => {
        if (e.target.files.length > 0) {
            handleFileSelect(e.target.files[0]);
        }
    });

    // Drag events
    ['dragenter', 'dragover'].forEach(eventName => {
        zone.addEventListener(eventName, (e) => {
            e.preventDefault();
            zone.classList.add('dragover');
        }, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        zone.addEventListener(eventName, (e) => {
            e.preventDefault();
            zone.classList.remove('dragover');
        }, false);
    });

    zone.addEventListener('drop', (e) => {
        const dt = e.dataTransfer;
        const files = dt.files;
        if (files.length > 0) {
            handleFileSelect(files[0]);
        }
    });
}

function handleFileSelect(file) {
    const ext = file.name.split('.').pop().toLowerCase();
    if (ext !== 'pdf' && ext !== 'docx') {
        alert("Invalid file format. Only PDF and DOCX files are supported.");
        return;
    }
    selectedFile = file;
    document.getElementById("upload-prompt").innerHTML = `
        <i class="bi bi-file-earmark-check-fill text-primary" style="font-size: 3rem;"></i>
        <p class="mt-2 fw-bold text-primary">${file.name}</p>
        <p class="text-muted small">${(file.size / 1024).toFixed(1)} KB - Click or drag to change</p>
    `;
    document.getElementById("btn-analyze").disabled = false;
}

// REST API Request to FastAPI
async function handleAnalyze() {
    if (!selectedFile) return;

    const jobDescription = document.getElementById("job-description").value.trim();
    
    // If no JD is provided, use a generic software engineer JD
    const jdToSend = jobDescription || "Looking for a professional with skills in python, javascript, sql, docker, git, communication, leadership, and project management.";

    const formData = new FormData();
    formData.append("file", selectedFile);
    formData.append("job_description", jdToSend);

    // Show Loader
    document.getElementById("loader").classList.remove("d-none");
    document.getElementById("btn-text-normal").classList.add("d-none");
    document.getElementById("btn-analyze").disabled = true;

    try {
        const response = await fetch(`${API_BASE_URL}/api/v1/compare-job-description`, {
            method: "POST",
            body: formData
        });

        if (!response.ok) {
            const errData = await response.json();
            throw new Error(errData.detail || "Failed to analyze resume.");
        }

        const data = await response.json();
        
        // Save to browser history
        saveToHistory(data);
        
        // Display Results
        displayResults(data);
        
    } catch (err) {
        alert("Error: " + err.message + "\n\nMake sure the backend server on Render is running and accessible.");
        console.error(err);
    } finally {
        // Hide Loader
        document.getElementById("loader").classList.add("d-none");
        document.getElementById("btn-text-normal").classList.remove("d-none");
        document.getElementById("btn-analyze").disabled = false;
    }
}

// Display Results Page
function displayResults(data) {
    // Switch views
    document.getElementById("upload-view").classList.add("d-none");
    document.getElementById("results-view").classList.remove("d-none");
    
    // Set text values
    document.getElementById("result-filename").innerText = data.filename;
    
    // Gauge score
    renderGauge(data.ats_score);
    
    // Details
    const ext = data.extracted_data || {};
    document.getElementById("result-emails").innerText = ext.emails?.join(", ") || "None found";
    document.getElementById("result-phones").innerText = ext.phones?.join(", ") || "None found";
    document.getElementById("result-experience").innerText = `${data.extracted_data?.experience_years || 0} Years`;
    
    // Skills
    const skillsList = document.getElementById("result-skills");
    skillsList.innerHTML = "";
    if (ext.skills && ext.skills.length > 0) {
        ext.skills.forEach(skill => {
            skillsList.innerHTML += `<span class="badge badge-skill m-1">${skill}</span>`;
        });
    } else {
        skillsList.innerHTML = `<span class="text-muted">None detected</span>`;
    }

    // Missing Skills
    const missingList = document.getElementById("result-missing-skills");
    missingList.innerHTML = "";
    if (data.missing_skills && data.missing_skills.length > 0) {
        data.missing_skills.forEach(skill => {
            missingList.innerHTML += `<span class="badge badge-missing-skill m-1">${skill}</span>`;
        });
    } else {
        missingList.innerHTML = `<span class="text-success fw-bold"><i class="bi bi-patch-check-fill"></i> No missing skills identified!</span>`;
    }

    // Certifications
    const certsList = document.getElementById("result-certifications");
    certsList.innerHTML = "";
    if (ext.certifications && ext.certifications.length > 0) {
        ext.certifications.forEach(cert => {
            certsList.innerHTML += `<span class="badge bg-secondary m-1">${cert}</span>`;
        });
    } else {
        certsList.innerHTML = `<span class="text-muted">None detected</span>`;
    }

    // Projects
    const projList = document.getElementById("result-projects");
    projList.innerHTML = "";
    if (ext.projects && ext.projects.length > 0) {
        ext.projects.forEach(proj => {
            projList.innerHTML += `<li>${proj}</li>`;
        });
    } else {
        projList.innerHTML = `<li>No specific projects extracted.</li>`;
    }

    // Education
    const eduList = document.getElementById("result-education");
    eduList.innerHTML = "";
    if (ext.education && ext.education.length > 0) {
        ext.education.forEach(edu => {
            eduList.innerHTML += `
                <div class="mb-2">
                    <strong>${edu.degree}</strong> - <span class="text-muted">${edu.major}</span>
                    <div class="small text-muted italic">${edu.raw_text}</div>
                </div>
            `;
        });
    } else {
        eduList.innerHTML = `<div class="text-muted">None detected</div>`;
    }

    // Recommendations
    const recsList = document.getElementById("result-recommendations");
    recsList.innerHTML = "";
    if (data.recommendations && data.recommendations.length > 0) {
        data.recommendations.forEach(rec => {
            recsList.innerHTML += `<div class="alert alert-info py-2 px-3 mb-2 small"><i class="bi bi-info-circle-fill me-2"></i>${rec}</div>`;
        });
    } else {
        recsList.innerHTML = `<div class="text-success small">Formatting and structural scores look excellent. No immediate recommendations.</div>`;
    }
}

// Render Animated SVG Gauge
function renderGauge(score) {
    const gaugeValue = document.getElementById("gauge-value");
    const gaugeProgress = document.getElementById("gauge-progress");
    
    // Set score color
    let color = "#ef4444"; // red
    if (score >= 80) color = "#22c55e"; // green
    else if (score >= 60) color = "#3b82f6"; // blue
    else if (score >= 40) color = "#f59e0b"; // yellow
    
    gaugeProgress.style.stroke = color;
    gaugeValue.style.color = color;
    
    // Dash calculations
    // Circumference = 2 * PI * r = 2 * 3.14159 * 70 = 439.8
    const circumference = 439.8;
    gaugeProgress.style.strokeDasharray = circumference;
    
    // Animate score from 0 to target
    let currentScore = 0;
    const interval = setInterval(() => {
        if (currentScore >= score) {
            clearInterval(interval);
        } else {
            currentScore++;
            gaugeValue.innerText = currentScore;
            const offset = circumference - (currentScore / 100) * circumference;
            gaugeProgress.style.strokeDashoffset = offset;
        }
    }, 15);
}

function resetToUploadView() {
    selectedFile = null;
    document.getElementById("upload-view").classList.remove("d-none");
    document.getElementById("results-view").classList.add("d-none");
    document.getElementById("btn-analyze").disabled = true;
    document.getElementById("job-description").value = "";
    document.getElementById("upload-prompt").innerHTML = `
        <i class="bi bi-cloud-arrow-up text-primary" style="font-size: 3.5rem;"></i>
        <h5 class="mt-3">Drag and drop your resume here</h5>
        <p class="text-muted">Supports PDF and DOCX files up to 5MB</p>
        <button type="button" class="btn btn-outline-primary btn-sm mt-2">Browse Files</button>
    `;
    loadHistory();
}

// Browser localStorage History Management
function saveToHistory(record) {
    let history = JSON.parse(localStorage.getItem("resume_analyzer_history")) || [];
    
    // Save only unique analyses
    history = history.filter(item => item.filename !== record.filename);
    history.unshift(record); // Add to beginning
    
    // Keep max 15 records
    if (history.length > 15) {
        history.pop();
    }
    
    localStorage.setItem("resume_analyzer_history", JSON.stringify(history));
}

function loadHistory() {
    const history = JSON.parse(localStorage.getItem("resume_analyzer_history")) || [];
    const tableBody = document.getElementById("history-table-body");
    const section = document.getElementById("history-section");
    
    if (history.length === 0) {
        section.classList.add("d-none");
        return;
    }
    
    section.classList.remove("d-none");
    tableBody.innerHTML = "";
    
    history.forEach(item => {
        const dateObj = new Date(item.date);
        const formattedDate = dateObj.toLocaleDateString() + " " + dateObj.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        
        let badgeClass = "badge-poor";
        if (item.ats_score >= 80) badgeClass = "badge-excellent";
        else if (item.ats_score >= 60) badgeClass = "badge-good";
        else if (item.ats_score >= 40) badgeClass = "badge-average";

        tableBody.innerHTML += `
            <tr>
                <td class="fw-bold">${item.filename}</td>
                <td><span class="badge ${badgeClass}">${item.ats_score}/100</span></td>
                <td>${formattedDate}</td>
                <td class="text-end">
                    <button class="btn btn-sm btn-outline-primary me-1" onclick="viewHistoryItem('${item.id}')"><i class="bi bi-eye"></i> View</button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteHistoryItem('${item.id}')"><i class="bi bi-trash"></i></button>
                </td>
            </tr>
        `;
    });
}

window.viewHistoryItem = function(id) {
    const history = JSON.parse(localStorage.getItem("resume_analyzer_history")) || [];
    const item = history.find(rec => rec.id === id);
    if (item) {
        displayResults(item);
    }
}

window.deleteHistoryItem = function(id) {
    if (!confirm("Are you sure you want to delete this analysis from your local history?")) return;
    
    let history = JSON.parse(localStorage.getItem("resume_analyzer_history")) || [];
    history = history.filter(rec => rec.id !== id);
    localStorage.setItem("resume_analyzer_history", JSON.stringify(history));
    loadHistory();
}
