// Theme switching functionality
document.addEventListener("DOMContentLoaded", () => {
    const themeToggler = document.getElementById("theme-toggler");
    if (themeToggler) {
        // Load active theme
        const currentTheme = localStorage.getItem("theme") || "light";
        document.documentElement.setAttribute("data-theme", currentTheme);
        updateTogglerIcon(themeToggler, currentTheme);

        themeToggler.addEventListener("click", () => {
            const currentTheme = document.documentElement.getAttribute("data-theme");
            const newTheme = currentTheme === "dark" ? "light" : "dark";
            
            document.documentElement.setAttribute("data-theme", newTheme);
            localStorage.setItem("theme", newTheme);
            updateTogglerIcon(themeToggler, newTheme);
        });
    }

    // Drag and Drop Upload Widget functionality
    const uploadZone = document.getElementById("upload-zone");
    const fileInput = document.getElementById("resume-file-input");
    const fileSelectBtn = document.getElementById("file-select-btn");
    const uploadPreview = document.getElementById("upload-preview");
    const uploadDetails = document.getElementById("upload-details");
    const removeFileBtn = document.getElementById("remove-file-btn");

    if (uploadZone && fileInput) {
        // Trigger file select dialog
        if (fileSelectBtn) {
            fileSelectBtn.addEventListener("click", (e) => {
                e.preventDefault();
                fileInput.click();
            });
        }

        // Handle file drop
        uploadZone.addEventListener("dragover", (e) => {
            e.preventDefault();
            uploadZone.classList.add("dragover");
        });

        uploadZone.addEventListener("dragleave", () => {
            uploadZone.classList.remove("dragover");
        });

        uploadZone.addEventListener("drop", (e) => {
            e.preventDefault();
            uploadZone.classList.remove("dragover");
            
            if (e.dataTransfer.files.length) {
                fileInput.files = e.dataTransfer.files;
                showFilePreview(fileInput.files[0]);
            }
        });

        fileInput.addEventListener("change", () => {
            if (fileInput.files.length) {
                showFilePreview(fileInput.files[0]);
            }
        });

        if (removeFileBtn) {
            removeFileBtn.addEventListener("click", (e) => {
                e.preventDefault();
                fileInput.value = "";
                if (uploadPreview) uploadPreview.classList.add("d-none");
                if (uploadZone) uploadZone.classList.remove("d-none");
            });
        }
    }

    function showFilePreview(file) {
        if (!file) return;
        const validExtensions = ["pdf", "docx"];
        const fileExt = file.name.split(".").pop().toLowerCase();
        
        if (!validExtensions.includes(fileExt)) {
            alert("Unsupported file format! Please upload a PDF or DOCX file.");
            fileInput.value = "";
            return;
        }

        if (uploadZone) uploadZone.classList.add("d-none");
        if (uploadPreview) {
            uploadPreview.classList.remove("d-none");
            const sizeInMb = (file.size / (1024 * 1024)).toFixed(2);
            if (uploadDetails) {
                uploadDetails.innerHTML = `
                    <div class="d-flex align-items-center justify-content-center mb-3">
                        <i class="bi bi-file-earmark-${fileExt === 'pdf' ? 'pdf text-danger' : 'word text-primary'} display-2 me-2"></i>
                        <div class="text-start">
                            <h5 class="mb-0 text-truncate" style="max-width: 300px;">${file.name}</h5>
                            <span class="text-muted small">${sizeInMb} MB • ${fileExt.toUpperCase()} Document</span>
                        </div>
                    </div>
                `;
            }
        }
    }

    function updateTogglerIcon(toggler, theme) {
        const icon = toggler.querySelector("i");
        if (icon) {
            if (theme === "dark") {
                icon.className = "bi bi-sun-fill text-warning";
            } else {
                icon.className = "bi bi-moon-fill text-secondary";
            }
        }
    }
});
