// Base URL for the Marvel team's API (running via their docker compose on port 8082).
const MARVEL_API_BASE = "http://localhost:8082";

// Maps a Marvel API comic payload to the shape our UI expects.
// Their field names differ from ours — same concept, different labels:
//   Marvel  id          → our comicId
//   Marvel  author      → our characterNames  (creator credited in the "featured" slot)
//   Marvel  description → our description     (straight through)
//   issueNumber / yearPublished have no Marvel equivalent — shown as "N/A" / "—"
function mapMarvelComic(raw) {
    return {
        comicId:        raw.id,
        title:          raw.title || "",
        issueNumber:    "N/A",
        yearPublished:  "",
        publisher:      "Marvel Comics",
        status:         "available",
        checkedOutBy:   null,
        characterIds:   [],
        characterNames: raw.author ? [raw.author] : [],
        description:    raw.description || "",
        source:         "marvel"
    };
}

async function fetchMarvelComics() {
    const response = await fetch(`${MARVEL_API_BASE}/api/comics`);
    if (!response.ok) throw new Error(`Marvel API returned ${response.status}.`);
    const data = await response.json();
    return data.map(mapMarvelComic);
}

async function fetchMarvelComic(id) {
    const response = await fetch(`${MARVEL_API_BASE}/api/comics/${id}`);
    if (!response.ok) throw new Error(`Marvel API returned ${response.status} for comic ${id}.`);
    return mapMarvelComic(await response.json());
}
