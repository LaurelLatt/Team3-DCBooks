const comicList = document.getElementById("comicList");
const titleFilter = document.getElementById("titleFilter");
const characterFilter = document.getElementById("characterFilter");
const publisherFilter = document.getElementById("publisherFilter");


let comics = [];

async function loadComics(){
    try {
        const response = await fetch("http://localhost:8080/api/comics");
        
        if (!response.ok) {
            throw new Error("Could not load comics from the database.");
        }
        
        comics = await response.json();
        renderComics(comics);
    } catch (error) {
        comicList.innerHTML = `
            <div class="col-12">
                <div class="alert alert-danger">
                    ${error.message}
                </div>
            </div>
        `;
    }
}

function renderComics(list) {
    if (list.length === 0) {
        comicList.innerHTML = `
            <div class="col-12">
                <div class="alert alert-warning">No comics found.</div>
            </div>
        `;
        return;
    }
    
    comicList.innerHTML = list.map(comic => ` 
        <div class="col-md-6 col-lg-4 mb-4">
            <div class="card h-100 shadow-sm" style="cursor:pointer"
                onClick="window.location.href='comic-detail.html?id=${comic.comicId}'">
                <div class="card-body">
                    <h5 class="card-title">${comic.title}</h5>
                    <p class="card-text mb-1"><strong>Issue:</strong> ${comic.issueNumber}</p>
                    <p class="card-text mb-1"><strong>Publisher:</strong> ${comic.publisher}</p>
                    <p class="card-text mb-1"><strong>Year:</strong> ${comic.yearPublished}</p>
                    <p class="card-text mb-2"><strong>Characters:</strong> ${comic.characterNames?.join(" , ") || "None listed"}</p>
                    <p class="card-text"> 
                        <strong>Status:</strong> ${comic.status === "available" ? "Available" : "Checked Out"}
                   </p>
                </div>
            </div>
        </div>
    `).join("");
}

function filterComics() {
    const titleValue = titleFilter.value.toLowerCase();
    const characterValue = characterFilter.value.toLowerCase();
    const publisherValue = publisherFilter.value.toLowerCase();
    
    const filtered = comics.filter(comic => {
        const matchesTitle = comic.title.toLowerCase().includes(titleValue);
        const matchesCharacter = (comic.characterNames || []).some(character =>
            character.toLowerCase().includes(characterValue)
        );
        const matchesPublisher = comic.publisher.toLowerCase().includes(publisherValue);
        
        return matchesTitle && matchesCharacter && matchesPublisher;
    });
    
    renderComics(filtered);
}

titleFilter.addEventListener("input", filterComics);
characterFilter.addEventListener("input", filterComics);
publisherFilter.addEventListener("input", filterComics);

loadComics();