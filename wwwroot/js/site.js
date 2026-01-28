// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Désactiver clic droit
document.addEventListener('contextmenu', event => event.preventDefault());

// Désactiver copier / coller / couper
document.addEventListener('copy', event => event.preventDefault());
document.addEventListener('cut', event => event.preventDefault());
document.addEventListener('paste', event => event.preventDefault());

// Désactiver raccourcis clavier
document.addEventListener('keydown', function(e) {
if (
    (e.ctrlKey && ['c','v','x','u','s','a'].includes(e.key.toLowerCase())) ||
    (e.metaKey && ['c','v','x','a'].includes(e.key.toLowerCase()))
) {
    e.preventDefault();
}
});

document.body.insertAdjacentHTML(
  'beforeend',
  '<div style="position:fixed;bottom:10px;right:10px;opacity:0.2;">Utilisateur : email@exemple.com</div>'
);


