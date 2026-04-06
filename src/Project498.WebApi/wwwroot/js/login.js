document.getElementById("loginForm").addEventListener("submit", async (e) => {
    e.preventDefault();

    const username = document.getElementById("floatingInput").value;
    const password = document.getElementById("floatingPassword").value;

    try {
        const response = await fetch("https://localhost:5001/api/users/login", {
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
            const user = await response.json();

            // store login state
            localStorage.setItem("loggedInUser", user.username);

            alert("Login successful!");
            window.location.href = "index.html";
        } else {
            alert("Invalid username or password");
        }

    } catch (error) {
        console.error("Error:", error);
        document.getElementById("errorMessage").textContent = "Invalid username or password";
    }
});