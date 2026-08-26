document.addEventListener("DOMContentLoaded", () => {
    // Set Favicon
    if (!document.querySelector("link[rel*='icon']")) {
        const favicon = document.createElement("link");
        favicon.rel = "icon";
        favicon.href = "/images/logo.png";
        favicon.type = "image/png";
        document.head.appendChild(favicon);
    }

    // Ignore layout injection on the Login page
    if (window.location.pathname.toLowerCase().includes("login")) return;

    // Get user role
    let userRole = "User";
    const token = localStorage.getItem("appToken");
    if (token) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            userRole = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.role || payload.Role || "User";
        } catch (e) {}
    }

    const links = [
        { path: "/Dashboard.html", icon: "📊", text: "لوحة التحكم" },
        { path: "/Registration.html", icon: "💳", text: "التسجيل والاشتراكات" },
        { path: "/StudentManage.html", icon: "✏️", text: "الطلاب" },
        { path: "/ClassStudents.html", icon: "📋", text: "قوائم الفصول" },
        { path: "/AttendanceEntry.html", icon: "✓", text: "تسجيل الحضور" },
        { path: "/AttendanceTrack.html", icon: "📅", text: "متابعة الحضور" },
        { path: "/Excuses.html", icon: "📝", text: "تقديم الأعذار" },
        { path: "/SubjectGrades.html", icon: "✏️", text: "رصد الدرجات" },
        { path: "/GradesReview.html", icon: "📝", text: "مراجعة الدرجات" },
        { path: "/ClassResults.html", icon: "🏆", text: "النتائج" },
        { path: "/Certificates.html", icon: "🎓", text: "الشهادات" },
        { path: "/IDCard.html", icon: "🪪", text: "الكارنيهات" },
        { path: "/Renewals.html", icon: "📞", text: "تجديد الاشتراكات" },
        { path: "/Statistics.html", icon: "📈", text: "الإحصائيات" },
        { path: "/Settings.html", icon: "⚙️", text: "الإعدادات" }
    ];

    if (userRole === "Admin") {
        links.push({ path: "/ManageExcuses.html", icon: "⚙️", text: "إدارة الأعذار" });
        links.push({ path: "/ManageRegistrations.html", icon: "📝", text: "إدارة التسجيلات" });
        links.push({ path: "/Permissions.html", icon: "🛡️", text: "الصلاحيات" });
        links.push({ path: "/Archive.html", icon: "📦", text: "الأرشيف" });
        links.push({ path: "/AuditLogs.html", icon: "📜", text: "سجل العمليات" });
    }

    const currentPath = window.location.pathname;

    // Build Sidebar HTML
    let sidebarHtml = `
        <div class="sidebar-overlay" id="sidebarOverlay"></div>
        <div class="sidebar">
            <div class="sidebar-header" style="text-align:center; padding-top:20px; padding-bottom:10px;">
                <img src="/images/logo.png" alt="شعار المدرسة" style="max-width: 100px; border-radius: 50%; margin-bottom:10px; background-color: white; padding: 5px;">
                <h2 style="margin: 0; font-size: 1.2rem;">نظام المدارس</h2>
            </div>
            <ul class="sidebar-menu">
                ${links.map(link => `
                    <li>
                        <a href="${link.path}" class="${currentPath.toLowerCase().includes(link.path.toLowerCase()) ? 'active' : ''}">
                            <span style="margin-left: 10px;">${link.icon}</span> ${link.text}
                        </a>
                    </li>
                `).join('')}
            </ul>
        </div>
    `;

    // Build Header HTML
    let pageTitle = links.find(l => currentPath.toLowerCase().includes(l.path.toLowerCase()))?.text || "نظام المدارس";
    let headerHtml = `
        <header class="top-header">
            <div style="display: flex; align-items: center; gap: 15px;">
                <button id="sidebarToggle" style="background: none; border: none; font-size: 24px; cursor: pointer; color: var(--primary);">☰</button>
                <div class="page-title">${pageTitle}</div>
            </div>
            <div class="user-info">
                <span id="userNameDisplay">مرحباً بك</span>
                <button class="logout-btn" onclick="logout()">تسجيل خروج</button>
            </div>
        </header>
    `;

    // Extract current main content
    const existingContent = document.body.innerHTML;

    // Reconstruct Body
    document.body.innerHTML = `
        <div id="layout-wrapper">
            ${sidebarHtml}
            <div class="main-wrapper">
                ${headerHtml}
                <main>
                    ${existingContent}
                </main>
            </div>
        </div>
    `;

    // Bind logout function
    window.logout = function() {
        localStorage.removeItem("appToken");
        window.location.href = "/Login.html";
    };

    // Bind sidebar toggle function
    const toggleBtn = document.getElementById("sidebarToggle");
    const sidebar = document.querySelector(".sidebar");
    const mainWrapper = document.querySelector(".main-wrapper");
    const overlay = document.getElementById("sidebarOverlay");

    function toggleSidebar() {
        sidebar.classList.toggle("collapsed");
        mainWrapper.classList.toggle("expanded");
        if (overlay) {
            overlay.classList.toggle("active");
        }
    }

    if (toggleBtn && sidebar && mainWrapper) {
        toggleBtn.addEventListener("click", toggleSidebar);
    }
    
    if (overlay) {
        overlay.addEventListener("click", toggleSidebar);
    }

    // Load user info
    const tokenData = localStorage.getItem("appToken");
    if (tokenData) {
        try {
            const claims = JSON.parse(atob(tokenData.split('.')[1]));
            const name = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || claims.unique_name || claims.name || "مستخدم";
            const role = claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || claims.role || claims.Role || "User";
            const roleName = role === "Admin" ? "أدمن" : "مستخدم";
            
            const displaySpan = document.getElementById("userNameDisplay");
            
            // Initial basic info
            displaySpan.innerHTML = `مرحباً، <strong>${name}</strong> <span style="font-size: 12px; background: #e2e8f0; color: #1e293b; padding: 2px 8px; border-radius: 6px; margin-right: 5px;">${roleName}</span>`;
            
            // Fetch classes to show what they are responsible for
            if (typeof fetchApi === 'function') {
                fetchApi('/users/me/classes').then(res => {
                    if (res && res.classes && res.classes.length > 0) {
                        let respText = "";
                        if (role === "Admin") {
                            respText = "كل الفصول والمراحل";
                        } else {
                            // If they have many classes, maybe they are stage managers
                            const stages = [...new Set(res.classes.map(c => c.stage).filter(s => s))];
                            if (stages.length === 1 && res.classes.length > 2) {
                                respText = `مرحلة ${stages[0]}`;
                            } else {
                                respText = res.classes.map(c => c.name).join('، ');
                            }
                        }
                        
                        displaySpan.innerHTML += `<div style="font-size: 11px; color: var(--text-muted); margin-top: 4px;">المسؤولية: ${respText}</div>`;
                    }
                }).catch(e => console.error(e));
            }

        } catch(e) {
            console.error(e);
        }
    }
});

