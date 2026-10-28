
    // Add JavaScript to close enlarged view when clicking the close button
    document.addEventListener('DOMContentLoaded', function() {
        // Get all close buttons
        const closeButtons = document.querySelectorAll('.enlarged-view-close');

        // Add click event to each close button
        closeButtons.forEach(button => {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation(); // Prevent event bubbling
                const enlargedView = this.parentElement;
                enlargedView.style.opacity = '0';
                enlargedView.style.visibility = 'hidden';
            });
        });

        // Add event listener to allow clicking anywhere on the overlay to close it
        const enlargedViews = document.querySelectorAll('.enlarged-view');
        enlargedViews.forEach(view => {
            view.addEventListener('click', function(e) {
                if (e.target === this) {
                    this.style.opacity = '0';
                    this.style.visibility = 'hidden';
                }
            });
        });

        // Prevent clicked images from closing the enlarged view
        const enlargedImages = document.querySelectorAll('.enlarged-view img');
        enlargedImages.forEach(img => {
            img.addEventListener('click', function(e) {
                e.stopPropagation();
            });
        });
    });
