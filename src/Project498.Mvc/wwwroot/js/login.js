document.getElementById("loginForm").addEventListener("submit", async (e) => {
    e.preventDefault();

    const username = document.getElementById("floatingInput").value;
    const password = document.getElementById("floatingPassword").value;

    try {
        const response = await fetch("/api/auth/login", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                username: username,
                password: password
            })
        });

        if (response.ok) {
            const auth = await response.json();

            // Persist JWT for protected API calls.
            localStorage.setItem("accessToken", auth.access_token ?? auth.token);

            alert("Login successful!");
            window.location.href = "index.html";
        } else {
            const error = await response.json().catch(() => ({}));
            document.getElementById("errorMessage").textContent = error.message || "Invalid username or password";
        }

    } catch (error) {
        console.error("Error:", error);
        document.getElementById("errorMessage").textContent = "Unable to reach server. Please try again.";
    }
});