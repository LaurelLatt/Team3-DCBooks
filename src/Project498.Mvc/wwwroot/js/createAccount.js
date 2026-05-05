// checks passwords match
document.getElementById("confirmPassword").addEventListener("input", () => {
    const password = document.getElementById("password").value;
    const confirmPassword = document.getElementById("confirmPassword").value;
    const errorDiv = document.getElementById("passwordError");

    if (confirmPassword && password !== confirmPassword) {
        errorDiv.textContent = "Passwords do not match";
    } else {
        errorDiv.textContent = "";
    }
});

document.getElementById("createAccountForm").addEventListener("submit", async (e) => {
    e.preventDefault();

    const password = document.getElementById("password").value;
    const confirmPassword = document.getElementById("confirmPassword").value;
    const errorDiv = document.getElementById("passwordError");

    if (password !== confirmPassword) {
        errorDiv.textContent = "Passwords do not match";
        return;
    }

    const user = {
        firstName: document.getElementById("firstName").value,
        lastName: document.getElementById("lastName").value,
        username: document.getElementById("username").value,
        email: document.getElementById("email").value,
        password: password
    };

    try {
        const response = await fetch("/api/auth/register", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(user)
        });

        // Handle backend bad responses
        if (response.ok) {
            alert("Account created!");
            window.location.href = "login.html";
        } else {
            const error = await response.json().catch(() => ({}));
            errorDiv.textContent = error.message || "Could not create account.";
        }

    } catch (err) {
        console.error(err);
        errorDiv.textContent = "Server error. Please try again later.";
    }
});