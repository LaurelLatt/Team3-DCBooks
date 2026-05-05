function getUserIdFromToken() {
    const token = localStorage.getItem("accessToken");
    if (!token) return null;

    try {
        const payload = JSON.parse(atob(token.split(".")[1]));

        //possible claim names
        return (
            payload.user_id ||
            payload.nameid ||
            payload.sub ||
            payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] ||
            null
        );
    } catch (error) {
        console.error("Invalid token:", error);
        return null;
    }
}

document.addEventListener("DOMContentLoaded", async () => {
    const notLoggedInDiv = document.getElementById("not-logged-in");
    const comicsListDiv = document.getElementById("comics-list");

    const token = localStorage.getItem("accessToken");
    const userId = getUserIdFromToken();

    console.log("Token found:", !!token);
    console.log("Decoded userId:", userId);

    if (!token || !userId) {
        notLoggedInDiv.classList.remove("d-none");
        return;
    }

    try {
        let checkouts = [];

        // user-specific endpoint first
        const userResponse = await fetch(`/api/checkouts/user/${userId}`, {
            headers: {
                Authorization: `Bearer ${token}`
            }
        });

        if (userResponse.ok) {
            checkouts = await userResponse.json();
        } else {
            console.warn("User checkout endpoint failed, trying fallback.");

            // get all checkouts and filter client-side
            const allResponse = await fetch(`/api/checkouts`, {
                headers: {
                    Authorization: `Bearer ${token}`
                }
            });

            if (!allResponse.ok) {
                const errorText = await allResponse.text();
                throw new Error(`Failed to load checkouts. ${errorText}`);
            }

            const allCheckouts = await allResponse.json();

            checkouts = allCheckouts.filter(c =>
                String(c.userId) === String(userId) &&
                (!c.returnDate) &&
                String(c.status).toLowerCase() !== "returned"
            );
        }

        console.log("Checkouts loaded:", checkouts);

        if (!checkouts || checkouts.length === 0) {
            comicsListDiv.innerHTML = `
                <div class="alert alert-info">
                    You have no checked out comics right now.
                </div>
            `;
            return;
        }

        for (const c of checkouts) {
            let comicTitle = `Comic #${c.comicId}`;

            try {
                const comicResponse = await fetch(`/api/comics/${c.comicId}`);
                if (comicResponse.ok) {
                    const comic = await comicResponse.json();
                    comicTitle = comic.title;
                }
            } catch (error) {
                console.error("Could not load comic title:", error);
            }

            const item = document.createElement("div");
            item.className = "list-group-item d-flex justify-content-between align-items-center";

            item.innerHTML = `
                <div>
                    <strong>Title:</strong> ${comicTitle}<br>
                    <small>Due: ${new Date(c.dueDate).toLocaleDateString()}</small>
                </div>
                <button class="btn btn-sm btn-danger">Return</button>
            `;

            const returnButton = item.querySelector("button");
            returnButton.addEventListener("click", async () => {
                await returnComic(c.checkoutId, item, returnButton, token);
            });

            comicsListDiv.appendChild(item);
        }

    } catch (err) {
        console.error("Error loading checkouts:", err);
        comicsListDiv.innerHTML = `
            <div class="alert alert-danger">
                Could not load checked out comics.
            </div>
        `;
    }
});

async function returnComic(checkoutId, element, button, token) {
    try {
        button.disabled = true;
        button.textContent = "Returning...";

        const response = await fetch(`/api/checkouts/${checkoutId}`, {
            method: "PUT",
            headers: {
                Authorization: `Bearer ${token}`
            }
        });

        if (response.ok) {
            element.remove();
        } else if (response.status === 409) {
            alert("This comic was already returned.");
            button.disabled = false;
            button.textContent = "Return";
        } else {
            alert("Failed to return comic.");
            button.disabled = false;
            button.textContent = "Return";
        }
    } catch (err) {
        console.error("Error returning comic:", err);
        alert("Something went wrong while returning the comic.");
        button.disabled = false;
        button.textContent = "Return";
    }
}