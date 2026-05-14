const comicList = document.getElementById("comicList");
const titleFilter = document.getElementById("titleFilter");
const characterFilter = document.getElementById("characterFilter");
const publisherFilter = document.getElementById("publisherFilter");

let comics = [];

/** Merge app checkout state so Marvel cards show Checked Out when on loan. */
async function applyMarvelCheckoutStatus(marvelList) {
    if (!marvelList.length) return marvelList;

    try {
        const res = await fetch("/api/checkouts/marvel/on-loan-ids");
        if (!res.ok) return marvelList;

        const data = await res.json();
        const ids = data.comicIds ?? data.ComicIds ?? [];
        const onLoan = new Set(ids.map(Number));

        return marvelList.map(c => {
            const id = Number(c.comicId);
            const loaned = onLoan.has(id);
            return {
                ...c,
                status: loaned ? "checked_out" : "available"
            };
        });
    } catch {
        return marvelList;
    }
}

async function loadComics() {
    comicList.innerHTML = `<div class="col-12"><div class="alert alert-info">Loading comics...</div></div>`;

    const [dcResult, marvelResult] = await Promise.allSettled([
        fetch("/api/comics")
            .then(r => { if (!r.ok) throw new Error("Could not load DC comics."); return r.json(); })
            .then(data => data.map(c => ({ ...c, source: "dc" }))),
        fetchMarvelComics()
    ]);

    if (dcResult.status === "rejected") {
        comicList.innerHTML = `
            <div class="col-12">
                <div class="alert alert-danger">${dcResult.reason.message}</div>
            </div>`;
        return;
    }

    let marvelRows = marvelResult.status === "fulfilled" ? marvelResult.value : [];
    marvelRows = await applyMarvelCheckoutStatus(marvelRows);

    comics = [
        ...dcResult.value,
        ...marvelRows
    ];

    comicList.innerHTML = "";

    if (marvelResult.status === "rejected") {
        comicList.innerHTML = `
            <div class="col-12">
                <div class="alert alert-warning">
                    Marvel comics could not be loaded. Showing DC catalog only.
                </div>
            </div>`;
    }

    renderComics(comics);
}

function renderComics(list) {
    const existingWarning = comicList.querySelector(".alert-warning");

    if (list.length === 0) {
        comicList.innerHTML = `<div class="col-12"><div class="alert alert-warning">No comics found.</div></div>`;
        return;
    }

    const cards = list.map(comic => `
        <div class="col-md-6 col-lg-4 mb-4">
            <div class="comic-card h-100" onclick="window.location.href='comic-detail.html?id=${comic.comicId}&source=${comic.source}'">
                <div class="comic-card-top">
                    <span class="badge ${comic.status === "available" ? "text-bg-success" : "text-bg-secondary"}">
                        ${comic.status === "available" ? "Available" : "Checked Out"}
                    </span>
                </div>
                <div class="comic-card-body">
                    <h5 class="card-title">${comic.title}</h5>
                    <p class="card-text mb-1"><strong>Issue:</strong> ${comic.issueNumber}</p>
                    <p class="card-text mb-1"><strong>Publisher:</strong> ${comic.publisher}</p>
                    <p class="card-text mb-1"><strong>Year:</strong> ${comic.yearPublished || "—"}</p>
                    <p class="card-text mb-3">
                        <strong>Characters:</strong> ${comic.characterNames?.join(", ") || "None listed"}
                    </p>
                    <button class="btn btn-outline-primary w-100">View Details</button>
                </div>
            </div>
        </div>
    `).join("");

    if (existingWarning) {
        const warningHtml = existingWarning.parentElement.outerHTML;
        comicList.innerHTML = warningHtml + cards;
    } else {
        comicList.innerHTML = cards;
    }
}

function filterComics() {
    const titleValue     = titleFilter.value.toLowerCase();
    const characterValue = characterFilter.value.toLowerCase();
    const publisherValue = publisherFilter.value.toLowerCase();

    const filtered = comics.filter(comic => {
        const matchesTitle     = comic.title.toLowerCase().includes(titleValue);
        const matchesCharacter = (comic.characterNames || []).some(c =>
            c.toLowerCase().includes(characterValue)
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
