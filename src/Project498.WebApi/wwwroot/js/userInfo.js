function getUserIdFromToken() {
    const token = localStorage.getItem("accessToken");
    if (!token) return null;

    const payload = JSON.parse(atob(token.split('.')[1]));
    console.log(payload);
    return payload.user_id;
}

const userId = getUserIdFromToken();

function checkPasswordMatch() {
    if (confirmPassword.value && newPassword.value !== confirmPassword.value) {
        matchError.textContent = "Passwords do not match";
    } else {
        matchError.textContent = "";
    }
}

function checkPasswordRepeat() {
    if (oldPassword.value && newPassword.value === oldPassword.value) {
        repeatError.textContent = "New password cannot be old password";
    } else {
        repeatError.textContent = "";
    }
}

window.addEventListener("DOMContentLoaded", async () => {
    try {
        const response = await fetch(`/api/users/${userId}`);
        const user = await response.json();

        // Fill form
        document.getElementById("firstName").value = user.firstName;
        document.getElementById("lastName").value = user.lastName;
        document.getElementById("username").value = user.username;
        document.getElementById("email").value = user.email;
        

    } catch (err) {
        console.error(err);
    }
});

document.getElementById("accountForm").addEventListener("input", async (e) => {
    e.preventDefault();

    const messageDiv = document.getElementById("message");

    const updatedUser = {
        userId: userId,
        firstName: document.getElementById("firstName").value,
        lastName: document.getElementById("lastName").value,
        username: document.getElementById("username").value,
        email: document.getElementById("email").value
    };

    try {
        const response = await fetch(`api/users/${userId}`, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(updatedUser)
        });

        if (response.ok) {
            messageDiv.textContent = "Account updated successfully!";
        } else {
            const error = await response.text();
            messageDiv.textContent = error;
        }

    } catch (err) {
        console.error(err);
        messageDiv.textContent = "Server error";
    }
});

const newUsernameInput = document.getElementById("newUsername");
const usernameErrorDiv = document.getElementById("usernameError");
const saveUsernameBtn = document.getElementById("saveUsername");

// Live validation as user types
newUsernameInput.addEventListener("input", async () => {
    const username = newUsernameInput.value.trim();

    if (!username) {
        usernameErrorDiv.textContent = "Username cannot be empty";
        saveUsernameBtn.disabled = true;
        return;
    }

    try {
        // Call your API to check if username exists
        const response = await fetch(`/api/users/check-username?username=${encodeURIComponent(username)}`);
        const data = await response.json();

        if (data.exists) {
            usernameErrorDiv.textContent = "Username already taken";
            saveUsernameBtn.disabled = true;
        } else {
            usernameErrorDiv.textContent = "";
            saveUsernameBtn.disabled = false;
        }
    } catch (err) {
        console.error(err);
        usernameErrorDiv.textContent = "Error checking username";
        saveUsernameBtn.disabled = true;
    }
});

// Save the new username
saveUsernameBtn.addEventListener("click", async () => {
    const newUsername = newUsernameInput.value.trim();

    if (!newUsername) return;

    try {
        const response = await fetch(`/api/users/${userId}`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username: newUsername })
        });

        if (response.ok) {
            usernameErrorDiv.textContent = "";
            alert("Username updated successfully!");
            // Optionally update the main form input too
            document.getElementById("username").value = newUsername;
            const modal = bootstrap.Modal.getInstance(document.getElementById("usernameModal"));
            modal.hide();
        } else {
            const errorText = await response.text();
            usernameErrorDiv.textContent = errorText;
        }
    } catch (err) {
        console.error(err);
        usernameErrorDiv.textContent = "Server error, try again later";
    }
});

const newEmailInput = document.getElementById("newEmail");
const emailErrorDiv = document.getElementById("emailError");
const saveEmailBtn = document.getElementById("saveEmail");

newEmailInput.addEventListener("input", async () => {
    const email = newEmailInput.value.trim();

    if (!email) {
        emailErrorDiv.textContent = "Email cannot be empty";
        saveEmailBtn.disabled = true;
        return;
    }

    try {
        const response = await fetch(`/api/users/check-email?email=${encodeURIComponent(email)}`);
        const data = await response.json();

        if (data.exists) {
            emailErrorDiv.textContent = "Email already in use";
            saveEmailBtn.disabled = true;
        } else {
            emailErrorDiv.textContent = "";
            saveEmailBtn.disabled = false;
        }
    } catch (err) {
        console.error(err);
        emailErrorDiv.textContent = "Error checking email";
        saveEmailBtn.disabled = true;
    }
});

saveEmailBtn.addEventListener("click", async () => {
    const newEmail = newEmailInput.value.trim();
    if (!newEmail) return;

    try {
        const response = await fetch(`/api/users/${userId}`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email: newEmail })
        });

        if (response.ok) {
            emailErrorDiv.textContent = "";
            alert("Email updated successfully!");
            document.getElementById("email").value = newEmail;
            const modal = bootstrap.Modal.getInstance(document.getElementById("emailModal"));
            modal.hide();
        } else {
            const errorText = await response.text();
            emailErrorDiv.textContent = errorText;
        }
    } catch (err) {
        console.error(err);
        emailErrorDiv.textContent = "Server error, try again later";
    }
});

newPassword.addEventListener("input", checkPasswordRepeat);

confirmPassword.addEventListener("input", checkPasswordMatch);

const passwordModal = document.getElementById("passwordModal");

passwordModal.addEventListener("hidden.bs.modal", () => {
    document.getElementById("oldPassword").value = "";
    document.getElementById("newPassword").value = "";
    document.getElementById("confirmPassword").value = "";
    document.getElementById("passwordMessage").textContent = "";
});

document.getElementById("savePassword").addEventListener("click", async () => {
    const payload = {
        userId: getUserIdFromToken(),
        oldPassword: document.getElementById("oldPassword").value,
        newPassword: document.getElementById("newPassword").value,
        confirmPassword: document.getElementById("confirmPassword").value
    };

    try {
        const res = await fetch("/api/users/change-password", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        const msg = await res.text();
        document.getElementById("passwordMessage").textContent = msg;

        if (res.ok) {
            const modal = bootstrap.Modal.getInstance(document.getElementById("passwordModal"));
            modal.hide();
        }

    } catch (err) {
        console.error(err);
        document.getElementById("passwordMessage").textContent = "Server error";
    }
});