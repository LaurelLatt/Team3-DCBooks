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

    const user = {
        firstName: document.getElementById("firstName").value,
        lastName: document.getElementById("lastName").value,
        username: document.getElementById("username").value,
        email: document.getElementById("email").value,
        password: password
    };

    try {
        const response = await fetch("https://localhost:5001/api/users", {
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
            const errorText = await response.text(); // backend sends the message
            errorDiv.textContent = errorText;         // show inline error instead of generic alert
        }

    } catch (err) {
        console.error(err);
        errorDiv.textContent = "Server error. Please try again later.";
    }
});