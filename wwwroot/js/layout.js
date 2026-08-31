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
    const path = window.location.pathname.toLowerCase();
    if (path.includes("login") || path.includes("portal")) return;

    // Get user role
    let userRole = "User";
    const token = localStorage.getItem("appToken");
    if (token) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            userRole = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.role || payload.Role || "User";
        } catch (e) {}
    }

    let links = [];
    
    // Check which module we are in
    const isTarbeyaModule = path.includes("tarbeya");

    // Global Route Guard: Prevent Tarbeya users from accessing School pages
    if (userRole.startsWith("Tarbeya") && !isTarbeyaModule) {
        window.location.href = "/TarbeyaHierarchy.html";
        return;
    }

    // Global Route Guard: Prevent School users from accessing Tarbeya pages
    if (!userRole.startsWith("Tarbeya") && userRole !== "Admin" && isTarbeyaModule) {
        window.location.href = "/Dashboard.html";
        return;
    }

    if (isTarbeyaModule) {
        links = [
            { path: "/TarbeyaHierarchy.html", icon: "⛪", text: "الهيكل التنظيمي" },
            { path: "/TarbeyaAreas.html", icon: "📍", text: "إدارة المناطق" },
            { path: "/TarbeyaUsers.html", icon: "🛡️", text: "إدارة الخدام والصلاحيات" },
            { path: "/TarbeyaStudents.html", icon: "🧑‍🎓", text: "إدارة المخدومين" },
            { path: "/TarbeyaAttendance.html", icon: "📋", text: "الغياب والحضور" },
            { path: "/TarbeyaVisitation.html", icon: "🙏", text: "الافتقاد الرعوي" },
            { path: "/TarbeyaTrips.html", icon: "🚌", text: "الرحلات والماليات" },
            { path: "/TarbeyaFinances.html", icon: "💰", text: "خزينة الأسرة" },
            { path: "/TarbeyaServants.html", icon: "🤝", text: "الخدام والمهام" },
            { path: "/TarbeyaMahragan.html", icon: "🎪", text: "المهرجان والأنشطة" },
            { path: "/TarbeyaPoints.html", icon: "⭐", text: "بنك النقط" },
            { path: "/TarbeyaFollowup.html", icon: "📱", text: "لوحة المتابعة" },
            { path: "/TarbeyaSpiritual.html", icon: "🕊️", text: "المتابعة الروحية" }
        ];
        if (userRole === "Admin") {
            links.unshift({ path: "/Portal.html", icon: "🏠", text: "البوابة الرئيسية (تبديل)" });
        }
    } else {
        links = [
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
            links.unshift({ path: "/Portal.html", icon: "🏠", text: "البوابة الرئيسية (تبديل)" });
            links.push({ path: "/ManageExcuses.html", icon: "⚙️", text: "إدارة الأعذار" });
            links.push({ path: "/ManageRegistrations.html", icon: "📝", text: "إدارة التسجيلات" });
            links.push({ path: "/Siblings.html", icon: "👨‍👩‍👧‍👦", text: "إدارة الإخوة" });
            links.push({ path: "/Permissions.html", icon: "🛡️", text: "الصلاحيات" });
            links.push({ path: "/Archive.html", icon: "📦", text: "الأرشيف" });
            links.push({ path: "/AuditLogs.html", icon: "📜", text: "سجل العمليات" });
        }
    }

    const currentPath = window.location.pathname;

    // Build Sidebar HTML
    let sidebarHtml = `
        <div class="sidebar-overlay" id="sidebarOverlay"></div>
        <div class="sidebar">
            <div class="sidebar-header" style="text-align:center; padding-top:20px; padding-bottom:10px;">
                <img src="/images/logo.png" alt="شعار المدرسة" style="max-width: 100px; border-radius: 50%; margin-bottom:10px; background-color: white; padding: 5px;">
                <h2 style="margin: 0; font-size: 1.2rem;">نظام ادارة المدرسة </h2>
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
            <div class="user-info" style="display: flex; align-items: center; gap: 15px;">
                <!-- Notification Bell -->
                <div class="dropdown position-relative">
                    <button class="btn btn-light rounded-circle p-2 position-relative" type="button" id="notifDropdown" data-bs-toggle="dropdown" aria-expanded="false" style="width: 40px; height: 40px;">
                        <i class="fas fa-bell text-muted"></i>
                        <span id="notifBadge" class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger" style="display:none; font-size: 0.6rem;">
                            0
                        </span>
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0" aria-labelledby="notifDropdown" id="notifList" style="width: 300px; max-height: 400px; overflow-y: auto;">
                        <li><h6 class="dropdown-header fw-bold">الإشعارات</h6></li>
                        <li><hr class="dropdown-divider"></li>
                        <li id="noNotifsItem"><span class="dropdown-item text-muted text-center small">لا توجد إشعارات جديدة</span></li>
                    </ul>
                </div>
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
            const userId = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || claims.nameid || claims.sub || "0";
            
            const displaySpan = document.getElementById("userNameDisplay");
            
            // Initial basic info
            displaySpan.innerHTML = `مرحباً، <strong>${name}</strong> <span style="font-size: 12px; background: #e2e8f0; color: #1e293b; padding: 2px 8px; border-radius: 6px; margin-right: 5px;">${roleName}</span>`;
            
            // Notifications Logic
            if (typeof fetchApi === 'function') {
                // Fetch unread notifications
                fetchApi('/Notifications/unread').then(res => {
                    if (res && res.success && res.notifications) {
                        updateNotificationUI(res.notifications);
                    }
                }).catch(e => console.error(e));

                // Setup SignalR connection if library is loaded
                if (typeof signalR !== 'undefined') {
                    const connection = new signalR.HubConnectionBuilder()
                        .withUrl("/notificationHub", { accessTokenFactory: () => localStorage.getItem("appToken") })
                        .withAutomaticReconnect()
                        .build();

                    connection.on("ReceiveNotification", function (notif) {
                        // Toast or alert can be added here
                        console.log("New Notification Received: ", notif);
                        
                        let badge = document.getElementById('notifBadge');
                        let count = parseInt(badge.innerText) || 0;
                        count++;
                        badge.innerText = count;
                        badge.style.display = 'block';

                        let list = document.getElementById('notifList');
                        document.getElementById('noNotifsItem')?.remove();
                        
                        let li = document.createElement('li');
                        li.innerHTML = `
                            <a class="dropdown-item border-bottom py-2" href="#" onclick="markNotifAsRead(${notif.id}, this)">
                                <div class="fw-bold text-primary" style="font-size:0.85rem;">${notif.title}</div>
                                <div class="text-muted text-wrap" style="font-size:0.75rem;">${notif.message}</div>
                                <div class="text-secondary mt-1" style="font-size:0.65rem;">الآن</div>
                            </a>
                        `;
                        list.insertBefore(li, list.children[2]); // Insert after header
                    });

                    connection.start().catch(err => console.error(err.toString()));
                }
            }
            
            // Fetch classes to show what they are responsible for
            if (typeof fetchApi === 'function' && !role.startsWith("Tarbeya")) {
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
            } else if (role.startsWith("Tarbeya")) {
                // Show Tarbeya Responsibility
                let tarbeyaResp = "التربية الكنسية";
                if (role === "TarbeyaFamilyAdmin") tarbeyaResp = "أمين أسرة";
                if (role === "TarbeyaServant") tarbeyaResp = "خادم أسرة";
                if (role === "TarbeyaGeneralAdmin") tarbeyaResp = "أمين خدمة عام";
                displaySpan.innerHTML += `<div style="font-size: 11px; color: var(--text-muted); margin-top: 4px;">المسؤولية: ${tarbeyaResp}</div>`;
            }

        } catch(e) {
            console.error(e);
        }
    }
});

function updateNotificationUI(notifs) {
    let badge = document.getElementById('notifBadge');
    let list = document.getElementById('notifList');
    
    if (notifs.length > 0) {
        badge.innerText = notifs.length;
        badge.style.display = 'block';
        document.getElementById('noNotifsItem')?.remove();
        
        notifs.forEach(n => {
            let li = document.createElement('li');
            li.innerHTML = `
                <a class="dropdown-item border-bottom py-2" href="#" onclick="markNotifAsRead(${n.id}, this)">
                    <div class="fw-bold text-primary" style="font-size:0.85rem;">${n.title}</div>
                    <div class="text-muted text-wrap" style="font-size:0.75rem;">${n.message}</div>
                    <div class="text-secondary mt-1" style="font-size:0.65rem;">${new Date(n.createdAt).toLocaleDateString('ar-EG')}</div>
                </a>
            `;
            list.appendChild(li);
        });
    }
}

async function markNotifAsRead(id, element) {
    event.preventDefault();
    try {
        await fetchApi(`/Notifications/${id}/read`, { method: 'PUT' });
        element.parentElement.remove();
        
        let badge = document.getElementById('notifBadge');
        let count = parseInt(badge.innerText) || 0;
        count--;
        if(count <= 0) {
            badge.style.display = 'none';
            badge.innerText = '0';
            document.getElementById('notifList').innerHTML += `<li id="noNotifsItem"><span class="dropdown-item text-muted text-center small">لا توجد إشعارات جديدة</span></li>`;
        } else {
            badge.innerText = count;
        }
    } catch(e) { console.error(e); }
}

