class ImageViewController {
    constructor() {
        this.currentSlide = 0;
        this.previousSlide = -1;
        this.slides;
        this.intervalID = -1;
    }

    showSlides() {
        for (var i = 0; i < this.slides.length; i++) {
            var slide = this.slides[i];
            if (i == this.currentSlide) {
                slide.classList.remove("fadeOut");
                slide.classList.add("fadeIn");
                slide.style.display = "block";
            } else if (i == this.previousSlide) {
                slide.classList.remove("fadeIn");
                slide.classList.add("fadeOut");
            } else {
                slide.classList.remove("fadeIn", "fadeOut");
                slide.style.display = "none";
            }
        }
    };

    slideNext() {
        if (this.slides.length > 1) {
            this.previousSlide = this.currentSlide;
            if (++this.currentSlide >= this.slides.length) {
                this.currentSlide = 0;
            }
            this.showSlides();
        }
    }

    slidePrev() {
        if (this.slides.length > 1) {
            this.previousSlide = this.currentSlide;
            if (--this.currentSlide < 0) {
                this.currentSlide = this.slides.length - 1;
            }
            this.showSlides();
        }
    }

    playSlides() {
        if (this.slides.length > 1) {
            if (this.intervalID > -1) {
                clearInterval(this.intervalID);
            }
            this.intervalID = setInterval(() => {
                this.previousSlide = this.currentSlide;
                this.slideNext();
            }, 10000);
        }
    }

    stopSlides() {
        if (this.intervalID > -1) {
            clearInterval(this.intervalID);
            this.intervalID = -1;
        }
    }

    initialize(images) {
        this.stopSlides();
        this.currentSlide = 0;
        this.previousSlide = -1;
        document.querySelectorAll(".slide").forEach((e) => e.remove());
        var container = document.querySelector("#slide-container");
        var index = 0;
        images.split(";").forEach((image) => {
            var img = document.createElement("img");
            img.src = "images/" + image;
            var div = document.createElement("div");
            div.classList.add("slide");
            div.id = "slide-" + index;
            div.appendChild(img);
            div.addEventListener("animationend", (e) => {
                if (e.animationName == "fadeOut") {
                    e.target.style.display = "none";
                }
            }, false);
            container.appendChild(div);
        });
        this.slides = document.querySelectorAll(".slide");
        this.showSlides();
    }
}

var imageViewController = new ImageViewController();

function initializeSlides(images) {
    console.log("Initializing: " + images);
    imageViewController.initialize(images);
}

function startSlideShow() {
    console.log("Start slide show");
    imageViewController.playSlides();
}

function stopSlideShow() {
    console.log("Stop slide show");
    imageViewController.stopSlides();
}

function nextSlide() {
    console.log("Next slide");
    imageViewController.slideNext();
}

function prevSlide() {
    console.log("Prev slide");
    imageViewController.slidePrev();
}
