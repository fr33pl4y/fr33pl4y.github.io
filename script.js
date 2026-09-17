/* script.js — makes the whole site work.
   It reads data/games.json, then handles the 70s/80s tabs (building the
   game grid), the credits loop, and the help panel.

   The file has two parts:
   1. FUNCTIONS — everything the site knows how to do. Nothing in this
      part runs by itself; it just describes what should happen.
   2. WIRE EVERYTHING UP — the very bottom of the file. This is the
      part that actually runs when the page loads: it hooks the
      functions above up to buttons, and starts things going. */

/* ---- DOM ELEMENTS ---------------------------------------------------- */
const tabs = document.querySelectorAll(".tab[data-tab]");
const gamesGrid = document.getElementById("games-grid");

const credits = document.querySelectorAll(".credit");
const helpToggle = document.getElementById("help-toggle");
const helpOverlay = document.getElementById("help-overlay");
const helpSteps = document.querySelectorAll(".help-step");

const screenshotOverlay = document.getElementById("screenshot-overlay");
const screenshotOverlayImg = document.getElementById("screenshot-overlay-img");

/* ---- STATE ------------------------------------------------------------ */
let games = {};   // filled in by start(), from data/games.json
                   // shape: { "70s": [ {name, screenshot, sourceCode, year}, ... ], "80s": [...] }
let helpTimers = [];
let creditNumber = 0;

/* ========================================================================
   1. FUNCTIONS
   ======================================================================== */

/* Load the games data, then open the 70s tab. */
async function start() {
  games = await (await fetch("data/games.json")).json();
  select("70s");
}

/* Decade tabs: mark the clicked button active and show that decade's
   games. */
function select(name) {
  tabs.forEach(button => button.classList.toggle("is-active", button.dataset.tab === name));
  renderGames(name);
}

/* Empty out the grid and rebuild it with one tile per game in
   games[decade]. To add a new game, nothing here needs to change —
   just add an entry to data/games.json. */
function renderGames(decade) {
  gamesGrid.innerHTML = "";

  const gameList = games[decade] || [];
  gameList.forEach(game => {
    gamesGrid.appendChild(createGameTile(game));
  });
}

/* Build one game tile: a square showing the screenshot, the game name,
   and the year in the corner.
   - Clicking the tile downloads the game's source code file.
   - Hovering the tile shows an enlarged screenshot in the center of
     the screen (see showScreenshot/hideScreenshot below). */
function createGameTile(game) {
  // The whole tile is a link. The "download" attribute tells the browser
  // to save the linked file instead of navigating to it.
  const tile = document.createElement("a");
  tile.className = "game-tile";
  tile.href = game.sourceCode;
  tile.download = "";

  // The screenshot is used as the tile's background image, so the name
  // and year can sit on top of it.
  tile.style.backgroundImage = `url("${game.screenshot}")`;

  const name = document.createElement("span");
  name.className = "game-tile__name";
  name.textContent = game.name;
  tile.appendChild(name);

  const year = document.createElement("span");
  year.className = "game-tile__year";
  year.textContent = game.year;
  tile.appendChild(year);

  tile.addEventListener("mouseenter", () => showScreenshot(game));
  tile.addEventListener("mouseleave", hideScreenshot);

  return tile;
}

/* Show the enlarged screenshot overlay for one game. */
function showScreenshot(game) {
  screenshotOverlayImg.src = game.screenshot;
  screenshotOverlayImg.alt = game.name;
  screenshotOverlay.hidden = false;
}

/* Hide the enlarged screenshot overlay. */
function hideScreenshot() {
  screenshotOverlay.hidden = true;
}

/* The help panel: intro shows right away, then the steps reveal one
   at a time. */
function openHelp() {
  helpOverlay.hidden = false;
  helpToggle.classList.add("is-active");
  helpTimers.forEach(clearTimeout);
  helpTimers = [];
  helpSteps.forEach(step => step.classList.remove("is-revealed"));
  helpSteps.forEach((step, i) => {
    helpTimers.push(setTimeout(() => step.classList.add("is-revealed"), 300 + i * 220));
  });
}

function closeHelp() {
  helpOverlay.hidden = true;
  helpToggle.classList.remove("is-active");
  helpTimers.forEach(clearTimeout);
  helpTimers = [];
}

/* The credits loop: show each .credit item in turn, forever.
   data-hold in the HTML says how long each one stays on screen. */
function nextCredit() {
  const item = credits[creditNumber % credits.length];
  creditNumber++;
  item.classList.add("is-showing");
  setTimeout(() => {
    item.classList.remove("is-showing");
    setTimeout(nextCredit, 500);
  }, item.dataset.hold);
}

/* ========================================================================
   2. WIRE EVERYTHING UP
   This is the part that actually runs when the page loads.
   ======================================================================== */

tabs.forEach(button => {
  button.onclick = () => select(button.dataset.tab);
});

helpToggle.onclick = () => (helpOverlay.hidden ? openHelp() : closeHelp());

/* Clicking the dark area around the panel closes it. */
helpOverlay.onclick = event => {
  if (event.target === helpOverlay) closeHelp();
};

ARCADETIMELINES.onclick = () => ( window.open("https://arcadetimelines.github.io/") );

start();
nextCredit();
