function toggleDropdown(event) {
    event.preventDefault();
    document.getElementById("dropdown1").classList.toggle("show");
    document.getElementById("dropdown2").classList.toggle("show");
    const arrow = event.currentTarget.querySelector(".dropdown-arrow");
    arrow.classList.toggle("rotated");
}

window.onclick = function (event) {
    const toggle = document.querySelector('.dropdown-toggle');
    const menu1 = document.getElementById("dropdown1");
    const menu2 = document.getElementById("dropdown2");

    if ((event.target !== toggle && !menu1.contains(event.target)) || (event.target !== toggle && !menu2.contains(event.target))) {
        menu1.classList.remove('show');
        menu2.classList.remove('show');
    }
};