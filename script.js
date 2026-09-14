/* script.js — makes the whole site work.
   It reads data/manifest.json and data/hall_of_fame.json, then handles the tabs,
   the corner numbers, the credits loop, the theme menu, and swaps the
   canvas area between the timeline viewer and the Hall of Fame.

   The file has two parts:
   1. FUNCTIONS — everything the site knows how to do. Nothing in this
      part runs by itself; it just describes what should happen.
   2. WIRE EVERYTHING UP — the very bottom of the file. This is the
      part that actually runs when the page loads: it hooks the
      functions above up to buttons, and starts things going. */

/* ---- DOM ELEMENTS ---------------------------------------------------- */
const tabs = document.querySelectorAll(".tab[data-tab]");

const credits = document.querySelectorAll(".credit");
const hud = document.querySelector(".hud");
const helpToggle = document.getElementById("help-toggle");
const helpOverlay = document.getElementById("help-overlay");
const helpSteps = document.querySelectorAll(".help-step");

const flagCounter = document.getElementById("flag-counter");
const neonLogo = document.querySelector(".neon-logo");


/* ---- STATE ------------------------------------------------------------ */
let view = "70s";       // which main tab is selected
let revealTimers = [];
let helpTimers = [];
let creditNumber = 0;

/* ========================================================================
   1. FUNCTIONS
   ======================================================================== */

/* Load both JSON files, fill in the version line, then open the 70s tab. */
async function start() {
  //manifest = await (await fetch("data/manifest.json")).json();
  //hallOfFame = await (await fetch("data/hall_of_fame.json")).json();
  //document.getElementById(".credit").textContent = "test";
  select("70s");
}



/* Main tabs: always show that tab's timeline (and close the Hall of Fame). */
function select(name) {
  view = name;
  const tab = manifest.tabs[name];
 

  tabs.forEach(button => button.classList.toggle("is-active", button.dataset.tab === name));
}



/* Redraw the ghost favicon in the given color (the same shape as the
   CSS ghosts), so the tab icon always matches the current theme. */
function updateFavicon(color) {
  const svg =
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 22 24">` +
    `<path fill="${color}" d="M0 24V11a11 11 0 0 1 22 0v13z"/>` +
    `<circle cx="6.5" cy="9.5" r="2.5" fill="#ffffff"/>` +
    `<circle cx="15.5" cy="9.5" r="2.5" fill="#ffffff"/>` +
    `</svg>`;
  document.getElementById("favicon").href = "data:image/svg+xml," + encodeURIComponent(svg);
}



/* The help panel: intro shows right away, then the 7 steps reveal one
   at a time, same rhythm as the Hall of Fame rows. */
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



let cKeyHeld = false;

function isCKey(event) {
  return event.code === "KeyC" || event.key.toLowerCase() === "c";
}

window.addEventListener("keydown", event => {
  if (isCKey(event)) cKeyHeld = true;
});

window.addEventListener("keyup", event => {
  if (isCKey(event)) cKeyHeld = false;
});

window.addEventListener("blur", () => { cKeyHeld = false; });

neonLogo.addEventListener("mouseenter", () => neonLogo.focus({ preventScroll: true }));

neonLogo.addEventListener("click", event => {
  if (event.shiftKey && cKeyHeld) flagCounter.hidden = !flagCounter.hidden;
});

start();





nextCredit();
