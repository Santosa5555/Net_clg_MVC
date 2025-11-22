// Admin Sidebar - Highlight Active Link
(function() {
    'use strict';
    
    // Get current path
    const currentPath = window.location.pathname.toLowerCase();
    
    // Find all sidebar links
    const sidebarLinks = document.querySelectorAll('#admin-sidebar .sidebar-link');
    
    sidebarLinks.forEach(link => {
        const linkPath = link.getAttribute('href')?.toLowerCase() || '';
        
        // Check if current path matches the link path
        if (currentPath.includes(linkPath) && linkPath !== '') {
            link.classList.add('active');
        }
        
        // Special handling for dashboard
        if (currentPath.includes('/admin/account/admindashboard') || 
            currentPath === '/admin' || 
            currentPath === '/admin/') {
            const dashboardLink = document.querySelector('a[href*="AdminDashboard"]');
            if (dashboardLink) {
                dashboardLink.classList.add('active');
            }
        }
    });
})();

