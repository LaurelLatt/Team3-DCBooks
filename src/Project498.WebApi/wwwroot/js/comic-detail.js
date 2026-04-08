const loadingState = document.getElementById("loadingState");
const errorState = document.getElementById("errorState");
const comicDetail = document.getElementById("comicDetail");

const comicTitle = document.getElementById("comicTitle");
const comicIssue = document.getElementById("comicIssue");
const comicPublisher = document.getElementById("comicPublisher");
const comicYear = document.getElementById("comicYear");
const comicStatus = document.getElementById("comicStatus");
const comicCheckedOutBy = document.getElementById("comicCheckedOutBy");
const comicCharacters = document.getElementById("comicCharacters");
const comicDescription = document.getElementById("comicDescription");

const checkoutButton = document.getElementById("checkoutButton");
const checkoutMessage = document.getElementById("checkoutMessage");

let currentComicId = null;
let currentComicStatus = null;

function getComicIdFromUrl() {
    const params = new URLSearchParams(window.location.search);
    return params.get("id");
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

async function loadComic(){
    const comicId = getComicIdFromUrl();
    
    if (!comicId) {
        showError("No comic ID was provided in the URL.");
        return;
    }
    
    currentComicId = comicId;
    
    try {
        const response = await fetch(`http://localhost:8080/api/comics/${comicId}`);
        
        if (!response.ok) {
            throw new Error("Could not load comic details.");
        }
        
        const comic = await response.json();
        
        loadingState.classList.add("d-none");
        errorState.classList.add("d-none");
        comicDetail.classList.remove("d-none");
        
        comicTitle.textContent = comic.title;
        comicIssue.textContent = comic.issueNumber;
        comicPublisher.textContent = comic.publisher;
        comicYear.textContent = comic.yearPublished;
        comicCheckedOutBy.textContent = comic.checkedOutBy ?? "Nobody";
        comicCharacters.textContent = comic.characterNames?.length
            ? comic.characterNames.join(", ")
            : "No characters listed.";
        
        currentComicStatus = comic.status;
        setStatusBadge(comic.status);
        
        //placeholder until backend includes real comic description field
        comicDescription.textContent = 
            `${comic.title} is issue #${comic.issueNumber}, published by ${comic.publisher} in ${comic.yearPublished}.`;
    } catch (error) {
        showError(error.message);
    }
}

async function checkoutComic(){
    checkoutMessage.innerHTML = "";
    
    const token = localStorage.getItem("accessToken");
    
    if (!token) {
        checkoutMessage.innerHTML = `
            <div class="alert alert-warning">
                You need to log in before checking out a comic.
            </div>
        `;
        return;
    }
    
    if (!currentComicId) {
        checkoutMessage.innerHTML = `
            <div class="alert alert-danger">
                No comic selected.
            </div>
        `;
        return;
    }

    try {
        const response = await fetch("http://localhost:8080/api/checkouts", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${token}`
            },
            body: JSON.stringify({
                comicId: Number(currentComicId)
            })
        });

        const data = await response.json().catch(() => null);

        if (!response.ok) {
            throw new Error(data?.message || "Checkout failed.");
        }

        checkoutMessage.innerHTML = `
            <div class="alert alert-success">
                Comic checked out successfully.
            </div>
        `;

        currentComicStatus = "checked_out";
        setStatusBadge(currentComicStatus);
        comicCheckedOutBy.textContent = "You";
    } catch (error) {
        checkoutMessage.innerHTML = `
            <div class="alert alert-danger">
                ${error.message}
            </div>
        `;
    }
}

checkoutButton.addEventListener("click", checkoutComic);
loadComic();