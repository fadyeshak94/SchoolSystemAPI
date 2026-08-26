const API_BASE = '/api'; // لأن الـ API والـ HTML على نفس السيرفر

// دالة مركزية للاتصال بالـ Backend
async function fetchApi(endpoint, method = 'GET', body = null) {
    const token = localStorage.getItem('appToken');
    const headers = { 'Content-Type': 'application/json' };
    
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    const options = { method, headers };
    if (body) {
        options.body = JSON.stringify(body);
    }

    try {
        const response = await fetch(`${API_BASE}${endpoint}`, options);
        
        // لو التوكن منتهي أو غير صالح
        if (response.status === 401 || response.status === 403) {
            localStorage.removeItem('appToken');
            window.location.href = '/Login.html';
            return null;
        }

        // لو الدالة بترجع ملف (زي الـ PDF والـ ZIP)
        const contentType = response.headers.get("content-type");
        if (contentType && contentType.indexOf("application/json") === -1) {
             return response; 
        }

        return await response.json();
    } catch (error) {
        console.error("API Error:", error);
        return { success: false, message: "حدث خطأ في الاتصال بالخادم." };
    }
}

// دالة مساعدة لعرض التنبيهات
function showStatus(elementId, type, message) {
    const bar = document.getElementById(elementId);
    if (!bar) return;
    bar.className = `status-bar ${type}`;
    bar.textContent = message;
    bar.style.display = 'block';
    if(type === 'success' || type === 'info') {
        setTimeout(() => { bar.style.display = 'none'; }, 5000);
    }
}

// تحميل الفصول للـ Dropdowns
async function loadClassesDropdown(selectElementId, includeAllOption = true) {
    const res = await fetchApi('/users/me/classes'); // نفترض وجود Endpoint ترجع فصول المستخدم الحالي
    const select = document.getElementById(selectElementId);
    select.innerHTML = includeAllOption ? '<option value="">-- اختر فصل --</option>' : '';
    
    if (res && res.classes) {
        let lastStage = null;
        let optGroup = null;

        const sortedClasses = res.classes.sort((a, b) => {
            if (!a.stage) return 1;
            if (!b.stage) return -1;
            return a.stage.localeCompare(b.stage) || a.name.localeCompare(b.name);
        });

        sortedClasses.forEach(c => {
            if (c.stage !== lastStage && c.stage) {
                optGroup = document.createElement('optgroup');
                optGroup.label = c.stage;
                select.appendChild(optGroup);
                lastStage = c.stage;
            }
            
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = `${c.name} (${c.stage})`;
            
            if (optGroup && c.stage) {
                optGroup.appendChild(opt);
            } else {
                select.appendChild(opt);
            }
        });
    }
}

// دالة التحقق من الصلاحيات
function requireRole(...allowedRoles) {
    const token = localStorage.getItem('appToken');
    if (!token) {
        window.location.href = '/Login.html';
        return false;
    }
    
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const role = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.role || payload.Role || "User";
        
        if (!allowedRoles.includes(role)) {
            alert('🚫 عذراً، ليس لديك صلاحية للوصول لهذه الصفحة.');
            window.location.href = '/ClassStudents.html';
            return false;
        }
        return true;
    } catch (e) {
        console.error("Token error:", e);
        window.location.href = '/Login.html';
        return false;
    }
}

// دالة مساعدة لتحميل الملفات من Base64
function downloadBase64(base64Data, filename, contentType) {
    const linkSource = `data:${contentType};base64,${base64Data}`;
    const downloadLink = document.createElement("a");
    downloadLink.href = linkSource;
    downloadLink.download = filename;
    downloadLink.click();
}

// دالة تسجيل الخروج
function logout() {
    localStorage.removeItem('appToken');
    window.location.href = '/Login.html';
}
