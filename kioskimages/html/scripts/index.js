(() => {
    var currentSlide = 0;
    var previousSlide = -1;
    var slides = document.querySelectorAll(".slide");

    for (var i = 0; i < slides.length; i++) {
        slides[i].addEventListener("animationend", (e) => {
            if (e.animationName == "fadeOut") {
                e.target.style.display = "none";
            }
        }, false);
    }

    showSlides(currentSlide);

    function showSlides(index) {
        if (index >= slides.length) {
            currentSlide = 0;
        }
        console.log(previousSlide, currentSlide);
        for (var i = 0; i < slides.length; i++) {
            slide = slides[i];
            console.log(slide.classList);
            if (i == currentSlide) {
                console.log(i, "remove fadeOut, add fadeIn");
                slide.classList.remove("fadeOut");
                slide.classList.add("fadeIn");
                slide.style.display = "block";
            } else if (i == previousSlide) {
                console.log(i, "remove fadeIn, add fadeOut");
                slide.classList.remove("fadeIn");
                slide.classList.add("fadeOut");
            } else {
                console.log(i, "remove fadeIn/Out, display none");
                slide.classList.remove("fadeIn", "fadeOut");
                slide.style.display = "none";
            }
        }
    };

    setInterval(() => {
        previousSlide = currentSlide;
        showSlides(++currentSlide);
    }, 10000);
})();
