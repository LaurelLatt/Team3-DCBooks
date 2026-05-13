const loadingState      = document.getElementById("loadingState");
const errorState        = document.getElementById("errorState");
const comicDetail       = document.getElementById("comicDetail");

const comicTitle        = document.getElementById("comicTitle");
const comicIssue        = document.getElementById("comicIssue");
const comicPublisher    = document.getElementById("comicPublisher");
const comicYear         = document.getElementById("comicYear");
const comicStatus       = document.getElementById("comicStatus");
const comicCheckedOutBy = document.getElementById("comicCheckedOutBy");
const comicCharacters   = document.getElementById("comicCharacters");
const comicDescription  = document.getElementById("comicDescription");

const checkoutButton  = document.getElementById("checkoutButton");
const checkoutMessage = document.getElementById("checkoutMessage");

let currentComicId     = null;
let currentComicStatus = null;
let currentComicSource = "dc";

function getUserIdFromToken() {
    const token = localStorage.getItem("accessToken");
    if (!token) return null;

    try {
        const payload = JSON.parse(atob(token.split(".")[1]));

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

function getCheckedOutByLabel(checkedOutByUserId) {
    if (checkedOutByUserId == null) return "Nobody";

    const me = getUserIdFromToken();
    return me != null && String(me) === String(checkedOutByUserId)
        ? "You"
        : "Another user";
}

function getParamsFromUrl() {
    const params = new URLSearchParams(window.location.search);
    return {
        id:     params.get("id"),
        source: params.get("source") || "dc"
    };
}

function showError(message) {
    loadingState.classList.add("d-none");
    comicDetail.classList.add("d-none");
    errorState.textContent = message;
    errorState.classList.remove("d-none");
}

function setStatusBadge(status) {
    const normalized = (status || "").toLowerCase();
    if (normalized === "available") {
        comicStatus.innerHTML = '<span class="badge text-bg-success">Available</span>';
        checkoutButton.disabled = false;
    } else {
        comicStatus.innerHTML = '<span class="badge text-bg-secondary">Checked Out</span>';
        checkoutButton.disabled = true;
    }
}

async function loadComic() {
    const { id: comicId, source } = getParamsFromUrl();

    if (!comicId) {
        showError("No comic ID was provided in the URL.");
        return;
    }

    currentComicId     = comicId;
    currentComicSource = source === "marvel" ? "marvel" : "dc";

    try {
        let comic;

        if (source === "marvel") {
            comic = await fetchMarvelComic(comicId);
        } else {
            const response = await fetch(`/api/comics/${comicId}`);
            if (!response.ok) throw new Error("Could not load comic details.");
            comic = await response.json();
        }

        loadingState.classList.add("d-none");
        errorState.classList.add("d-none");
        comicDetail.classList.remove("d-none");

        comicTitle.textContent        = comic.title;
        comicIssue.textContent        = comic.issueNumber;
        comicPublisher.textContent    = comic.publisher;
        comicYear.textContent         = comic.yearPublished || "—";
        comicCheckedOutBy.textContent = getCheckedOutByLabel(comic.checkedOutBy);
        comicCharacters.textContent   = comic.characterNames?.length
            ? comic.characterNames.join(", ")
            : "No characters listed.";

        if (source === "marvel") {
            comicDescription.textContent = comic.description
                || `${comic.title} is a Marvel comic.`;
            checkoutMessage.innerHTML = "";

            try {
                const availRes = await fetch(`/api/checkouts/marvel/${comicId}/availability`);
                if (availRes.ok) {
                    const { available } = await availRes.json();
                    if (!available) {
                        currentComicStatus = "checked_out";
                        setStatusBadge("checked_out");
                        comicCheckedOutBy.textContent = "Another user";

                        const token = localStorage.getItem("accessToken");
                        if (token) {
                            const mineRes = await fetch(`/api/checkouts/marvel/${comicId}/availability/me`, {
                                headers: { "Authorization": `Bearer ${token}` }
                            });
                            if (mineRes.ok) {
                                const mine = await mineRes.json();
                                if (mine?.isMine) {
                                    comicCheckedOutBy.textContent = "You";
                                }
                            }
                        }
                    } else {
                        currentComicStatus = "available";
                        setStatusBadge("available");
                        comicCheckedOutBy.textContent = "Nobody";
                    }
                } else {
                    currentComicStatus = "available";
                    setStatusBadge("available");
                    comicCheckedOutBy.textContent = "Nobody";
                }
            } catch {
                currentComicStatus = "available";
                setStatusBadge("available");
                comicCheckedOutBy.textContent = "Nobody";
            }
        } else {
            comicDescription.textContent =
                `${comic.title} is issue #${comic.issueNumber}, published by ${comic.publisher} in ${comic.yearPublished}.`;
            currentComicStatus = comic.status;
            setStatusBadge(comic.status);

            // Never show an ID — only "Nobody", "You", or "Another user".
            if ((comic.status || "").toLowerCase() !== "available") {
                comicCheckedOutBy.textContent = getCheckedOutByLabel(comic.checkedOutBy);
            } else {
                comicCheckedOutBy.textContent = "Nobody";
            }
        }

    } catch (error) {
        showError(error.message);
    }
}

async function checkoutComic() {
    checkoutMessage.innerHTML = "";

    const token = localStorage.getItem("accessToken");
    if (!token) {
        checkoutMessage.innerHTML = `
            <div class="alert alert-warning">
                You need to log in before checking out a comic.
            </div>`;
        return;
    }

    if (!currentComicId) {
        checkoutMessage.innerHTML = `<div class="alert alert-danger">No comic selected.</div>`;
        return;
    }

    try {
        const response = await fetch("/api/checkouts", {
            method: "POST",
            headers: {
                "Content-Type":  "application/json",
                "Authorization": `Bearer ${token}`
            },
            body: JSON.stringify({
                comicId: Number(currentComicId),
                comicSource: currentComicSource
            })
        });

        const data = await response.json().catch(() => null);

        if (!response.ok) {
            throw new Error(data?.message || "Checkout failed.");
        }

        checkoutMessage.innerHTML = `<div class="alert alert-success">Comic checked out successfully.</div>`;
        currentComicStatus = "checked_out";
        setStatusBadge(currentComicStatus);
        comicCheckedOutBy.textContent = "You";

    } catch (error) {
        checkoutMessage.innerHTML = `<div class="alert alert-danger">${error.message}</div>`;
    }
}

checkoutButton.addEventListener("click", checkoutComic);
loadComic();
