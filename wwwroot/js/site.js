function toggleDropdown(event) {
    event.preventDefault();

    const container = document.getElementById("dropdown-container");
    container.classList.toggle("show");

    const arrow = event.currentTarget.querySelector(".dropdown-arrow");
    arrow.classList.toggle("rotated");
}

window.onclick = function (event) {
    const toggle = document.querySelector('.dropdown-toggler');
    const container = document.getElementById("dropdown-container");

    if (!toggle.contains(event.target) && !container.contains(event.target)) {
        container.classList.remove('show');
        const arrow = toggle.querySelector(".dropdown-arrow");
        if (arrow) arrow.classList.remove("rotated");
    }
};

function checkArduinoStatus() {
    fetch("/Arduino/IsConnected")
        .then(res => res.json())
        .then(data => {
            const dot = document.getElementById("statusDot");
            const text = document.getElementById("statusText");
            if (data.connected) {
                dot.style.backgroundColor = "lime";
                text.textContent = "Fingerprint reader connected and ready";
            } else {
                dot.style.backgroundColor = "red";
                text.textContent = "Waiting for device connection...";
            }
        })
        .catch(() => {
            // Handle errors or failed fetch (maybe disconnected)
            const dot = document.getElementById("statusDot");
            const text = document.getElementById("statusText");
            dot.style.backgroundColor = "red";
            text.textContent = "Disconnected";
        });
}

// Initial check + interval
checkArduinoStatus();
setInterval(checkArduinoStatus, 5000); // every 5 seconds
