function logout() {
    // remove auth data
    localStorage.removeItem("accessToken");
    localStorage.removeItem("user");

    // redirect
    window.location.href = "/login.html";
}

window.logout = logout;