const comicList = document.getElementById("comicList");
const titleFilter = document.getElementById("titleFilter");
const characterFilter = document.getElementById("characterFilter");
const publisherFilter = document.getElementById("publisherFilter");


let comics = [];

async function loadComics(){
    try {
        const response = await fetch("/api/comics");
        
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
            <div class="comic-card h-100" onclick="window.location.href='comic-detail.html?id=${comic.comicId}'">
                <div class="comic-card-top">
                    <span class="badge ${comic.statuc === "available" ? "text-bg-success" : "text-bg-secondary"}">
                        ${comic.status === "available" ? "Available" : "Checked Out"}
                    </span>  
                </div>                  
                
                <div class="comic-card-body">
                    <h5 class="card-title">${comic.title}</h5>
                    
                    <p class="card-text mb-1"><strong>Issue:</strong> ${comic.issueNumber}</p>
                    <p class="card-text mb-1"><strong>Publisher:</strong> ${comic.publisher}</p>
                    <p class="card-text mb-1"><strong>Year:</strong> ${comic.yearPublished}</p>
                    <p class="card-text mb-3">
                        <strong>Characters:</strong> ${comic.characterNames?.join(" , ") || "None listed"}</p>
                   <button class="btn btn-outline-primary w-100">
                        View Details
                    </button>
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