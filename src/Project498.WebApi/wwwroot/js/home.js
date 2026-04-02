const comicList = document.getElementById("comicList");
const titleFilter = document.getElementById("titleFilter");
const characterFilter = document.getElementById("characterFilter");
const publisherFilter = document.getElementById("publisherFilter");

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
                onClick="window.location.href='comic-detail.html?id=${comic.id}'">
                <div class="card-body">
                    <h5 class="card-title">${comic.title}</h5>
                    <p class="card-text mb-1"><strong>Issue:</strong> ${comic.issueNumber}</p>
                    <p class="card-text mb-1"><strong>Publisher:</strong> ${comic.publisher}</p>
                    <p class="card-text mb-1"><strong>Year:</strong> ${comic.year}</p>
                    <p class="card-text mb-2"><strong>Characters:</strong> ${comic.characters.join(" , ")}</p>
                    <p class="card-text"> 
                        <strong>Status:</strong> ${comic.available ? "Available" : "Checked Out"}
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
        const matchesCharacter = comic.characters.some(character =>
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

renderComics(comics);