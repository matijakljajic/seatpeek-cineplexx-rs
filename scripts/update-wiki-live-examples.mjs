import fs from "node:fs";
import path from "node:path";

const [wikiPagePath, samplesDirectory] = process.argv.slice(2);

if (!wikiPagePath || !samplesDirectory) {
  throw new Error("Usage: node update-wiki-live-examples.mjs <wiki-page-path> <samples-directory>");
}

const findSample = prefix => {
  const files = fs.readdirSync(samplesDirectory).filter(file => file.startsWith(prefix) && file.endsWith(".json"));
  if (files.length !== 1) throw new Error(`Expected one ${prefix}*.json sample, found: ${files.join(", ") || "none"}`);
  return path.join(samplesDirectory, files[0]);
};

const replaceOne = (page, pattern, replacement, description) => {
  const matches = [...page.matchAll(pattern)];
  if (matches.length !== 1) throw new Error(`Expected one ${description} example, found ${matches.length}.`);
  return page.replace(pattern, replacement);
};

const cinema = JSON.parse(fs.readFileSync(path.join(samplesDirectory, "cinema.json"), "utf8"));
const locations = JSON.parse(fs.readFileSync(path.join(samplesDirectory, "locations.json"), "utf8"));
const programme = JSON.parse(fs.readFileSync(findSample("cinema-sessions-"), "utf8"));
const sessionSample = JSON.parse(fs.readFileSync(findSample("session-"), "utf8"));
const session = sessionSample.session;
const movie = sessionSample.scheduledFilm;
const location = locations.find(value => value.items.includes(Number(cinema.id)));
const programmeDay = Array.isArray(programme) ? programme[0] : programme;
const date = programmeDay?.date?.slice(0, 10);
const movieSlug = movie.slug;
const monthStart = `${date?.slice(0, 8)}01`;

console.log({ cinemaId: cinema.id, locationId: location?.id, programmeDate: date, movieSlug });

const missing = [!location && "location", !date && "programme date", !movieSlug && "movie slug"].filter(Boolean);
if (missing.length > 0) throw new Error(`Refreshed samples do not contain: ${missing.join(", ")}.`);

const baseUrl = "https://app.cineplexx.rs/api";
let page = fs.readFileSync(wikiPagePath, "utf8");

page = replaceOne(page,
  /Example using cinema `[^`]+`:\n\nhttps:\/\/app\.cineplexx\.rs\/api\/v1\/cinemas\/\d+(?=\n)/g,
  `Example using cinema \`${cinema.id}\`:\n\n${baseUrl}/v1/cinemas/${cinema.id}`,
  "cinema-details");
page = replaceOne(page,
  /Example using cinema `[^`]+`:\n\nhttps:\/\/app\.cineplexx\.rs\/api\/v1\/cinemas\/[^\s]+\/sessions/g,
  `Example using cinema \`${cinema.id}\`:\n\n${baseUrl}/v1/cinemas/${cinema.id}/sessions`,
  "cinema-sessions");
page = replaceOne(page,
  /For example, if a cinema programme returns:\n\n```text\ncinemaId  = [^\n]+\nsessionId = [^\n]+\n```\n\nthe corresponding session key is:\n\n```text\n[^\n]+\n```\n\nand the request would be:\n\nhttps:\/\/app\.cineplexx\.rs\/api\/v1\/sessions\/[^\s]+/g,
  ["For example, if a cinema programme returns:", "", "```text", `cinemaId  = ${cinema.id}`, `sessionId = ${session.sessionId}`, "```", "", "the corresponding session key is:", "", "```text", session.id, "```", "", "and the request would be:", "", `${baseUrl}/v1/sessions/${session.id}`].join("\n"),
  "session-details");
page = replaceOne(page,
  /For:\n\n```text\ncinemaId  = [^\n]+\nsessionId = [^\n]+\n```\n\nthe request would be:\n\nhttps:\/\/app\.cineplexx\.rs\/api\/v1\/seat-plan\/[^\s]+/g,
  ["For:", "", "```text", `cinemaId  = ${cinema.id}`, `sessionId = ${session.sessionId}`, "```", "", "the request would be:", "", `${baseUrl}/v1/seat-plan/${cinema.id}/${session.sessionId}`].join("\n"),
  "seat-plan");
page = replaceOne(page,
  /Using \*\*[^*]+\*\*:\n\n```text\nmovieId = [^\n]+\nslug    = [^\n]+\n```\n\nBy ID:\n\nhttps:\/\/app\.cineplexx\.rs\/api\/v1\/movies\/[^\s]+\n\nBy slug:\n\nhttps:\/\/app\.cineplexx\.rs\/api\/v1\/movies\/[^\s]+/g,
  [`Using **${movie.title}**:`, "", "```text", `movieId = ${session.movieId}`, `slug    = ${movieSlug}`, "```", "", "By ID:", "", `${baseUrl}/v1/movies/${session.movieId}`, "", "By slug:", "", `${baseUrl}/v1/movies/${movieSlug}`].join("\n"),
  "movie-details");
page = replaceOne(page,
  /https:\/\/app\.cineplexx\.rs\/api\/v2\/movies\?date=[^&\s]+&location=\d+/g,
  `${baseUrl}/v2/movies?date=${date}&location=${location.id}`,
  "filtered-programme");
page = replaceOne(page,
  /https:\/\/app\.cineplexx\.rs\/api\/v2\/movies\/top\?date=[^&\s]+&location=\d+/g,
  `${baseUrl}/v2/movies/top?date=${date}&location=${location.id}`,
  "recommended-movies");
page = replaceOne(page,
  /https:\/\/app\.cineplexx\.rs\/api\/v2\/movies\/coming-soon\?date=[^&\s]+&location=all/g,
  `${baseUrl}/v2/movies/coming-soon?date=${monthStart}&location=all`,
  "upcoming-movies");
page = replaceOne(page,
  /Example using location `\d+`:\n\nhttps:\/\/app\.cineplexx\.rs\/api\/v2\/movies\/filters\/dates\/list\?location=\d+/g,
  `Example using location \`${location.id}\`:\n\n${baseUrl}/v2/movies/filters/dates/list?location=${location.id}`,
  "programme-dates");
page = replaceOne(page,
  /https:\/\/app\.cineplexx\.rs\/api\/v2\/movies\/filters\/dates\/list\?top=true&location=\d+/g,
  `${baseUrl}/v2/movies/filters/dates/list?top=true&location=${location.id}`,
  "recommended-movie-dates");
page = replaceOne(page,
  /https:\/\/app\.cineplexx\.rs\/api\/v1\/events\?location=\d+/g,
  `${baseUrl}/v1/events?location=${location.id}`,
  "events");

fs.writeFileSync(wikiPagePath, page);
