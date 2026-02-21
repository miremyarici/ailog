// ========================================
// Profile Edit Mode - profile.js
// ========================================

let isEditMode = false;
let originalBioText = '';
let originalAvatarSrc = '';
let originalCoverSrc = '';
let avatarFile = null;
let coverFile = null;

// Toggle edit mode
function toggleEditMode() {
    isEditMode = true;
    const container = document.getElementById('profileContainer');
    container.classList.add('editing');

    // Store originals
    const bioText = document.getElementById('profileBioText');
    originalBioText = bioText ? bioText.textContent.trim() : '';

    const avatarImg = document.getElementById('avatarImage');
    originalAvatarSrc = avatarImg ? avatarImg.src : '';

    const coverImg = document.getElementById('coverImage');
    originalCoverSrc = coverImg ? coverImg.src : '';

    // Show/hide buttons
    document.getElementById('editProfileBtn').style.display = 'none';
    document.getElementById('profileEditActions').style.display = 'flex';

    // Reset file selections
    avatarFile = null;
    coverFile = null;
}

// Cancel edit mode
function cancelEditMode() {
    isEditMode = false;
    const container = document.getElementById('profileContainer');
    container.classList.remove('editing');

    // Restore original avatar
    const avatarImg = document.getElementById('avatarImage');
    if (avatarImg && originalAvatarSrc) {
        avatarImg.src = originalAvatarSrc;
    }

    // Restore original cover
    const coverImg = document.getElementById('coverImage');
    if (coverImg && originalCoverSrc) {
        coverImg.src = originalCoverSrc;
    }

    // Restore bio
    const bioTextarea = document.getElementById('profileBioTextarea');
    const bioDisplay = document.getElementById('profileBioDisplay');
    bioTextarea.style.display = 'none';
    bioDisplay.style.display = '';

    // Reset file inputs
    document.getElementById('avatarFileInput').value = '';
    document.getElementById('coverFileInput').value = '';
    avatarFile = null;
    coverFile = null;

    // Show/hide buttons
    document.getElementById('editProfileBtn').style.display = '';
    document.getElementById('profileEditActions').style.display = 'none';
}

// Save profile via Ajax POST
function saveProfile() {
    const formData = new FormData();

    // Add avatar file if changed
    if (avatarFile) {
        formData.append('avatar', avatarFile);
    }

    // Add cover file if changed
    if (coverFile) {
        formData.append('coverPhoto', coverFile);
    }

    // Add bio text
    const bioTextarea = document.getElementById('profileBioTextarea');
    const bioDisplay = document.getElementById('profileBioDisplay');
    if (bioTextarea.style.display !== 'none') {
        formData.append('bio', bioTextarea.value);
    } else {
        const bioText = document.getElementById('profileBioText');
        formData.append('bio', bioText ? bioText.textContent.trim() : '');
    }

    // Disable save button during request
    const saveBtn = document.getElementById('saveEditBtn');
    saveBtn.disabled = true;
    saveBtn.textContent = 'Saving...';

    $.ajax({
        url: '/Home/UpdateProfile',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (response) {
            if (response.success) {
                // Update avatar if changed
                if (response.avatarUrl) {
                    let avatarImg = document.getElementById('avatarImage');
                    const placeholder = document.getElementById('avatarPlaceholder');
                    if (placeholder) {
                        // Replace placeholder with img
                        const img = document.createElement('img');
                        img.src = response.avatarUrl;
                        img.alt = 'Profile';
                        img.className = 'profile-avatar-large';
                        img.id = 'avatarImage';
                        placeholder.parentNode.replaceChild(img, placeholder);
                    } else if (avatarImg) {
                        avatarImg.src = response.avatarUrl;
                    }
                }

                // Update cover if changed
                if (response.coverPhotoUrl) {
                    let coverImg = document.getElementById('coverImage');
                    const coverPlaceholder = document.getElementById('coverPlaceholder');
                    if (coverPlaceholder) {
                        // Replace placeholder with img
                        const img = document.createElement('img');
                        img.src = response.coverPhotoUrl;
                        img.alt = 'Cover Photo';
                        img.className = 'cover-image';
                        img.id = 'coverImage';
                        coverPlaceholder.parentNode.replaceChild(img, coverPlaceholder);
                    } else if (coverImg) {
                        coverImg.src = response.coverPhotoUrl;
                    }
                }

                // Update bio text
                if (bioTextarea.style.display !== 'none') {
                    const bioText = document.getElementById('profileBioText');
                    if (bioText) {
                        bioText.textContent = bioTextarea.value;
                    }
                    bioTextarea.style.display = 'none';
                    bioDisplay.style.display = '';
                    // Remove empty class if bio was added
                    if (bioTextarea.value.trim()) {
                        bioDisplay.classList.remove('profile-bio-empty');
                    }
                }

                // Exit edit mode
                isEditMode = false;
                const container = document.getElementById('profileContainer');
                container.classList.remove('editing');
                document.getElementById('editProfileBtn').style.display = '';
                document.getElementById('profileEditActions').style.display = 'none';
                document.getElementById('avatarFileInput').value = '';
                document.getElementById('coverFileInput').value = '';
                avatarFile = null;
                coverFile = null;
            }
        },
        error: function () {
            alert('An error occurred while saving the profile. Please try again.');
        },
        complete: function () {
            saveBtn.disabled = false;
            saveBtn.textContent = 'Save';
        }
    });
}

// DOM ready event handlers
document.addEventListener('DOMContentLoaded', function () {

    // Cover photo edit click
    const coverEditBtn = document.getElementById('coverEditBtn');
    if (coverEditBtn) {
        coverEditBtn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            document.getElementById('coverFileInput').click();
        });
    }

    // Cover file input change - preview
    const coverFileInput = document.getElementById('coverFileInput');
    if (coverFileInput) {
        coverFileInput.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (file) {
                coverFile = file;
                const reader = new FileReader();
                reader.onload = function (ev) {
                    let coverImg = document.getElementById('coverImage');
                    const coverPlaceholder = document.getElementById('coverPlaceholder');
                    if (coverPlaceholder) {
                        // Replace placeholder with a preview img
                        const img = document.createElement('img');
                        img.src = ev.target.result;
                        img.alt = 'Cover Photo';
                        img.className = 'cover-image';
                        img.id = 'coverImage';
                        coverPlaceholder.parentNode.replaceChild(img, coverPlaceholder);
                    } else if (coverImg) {
                        coverImg.src = ev.target.result;
                    }
                };
                reader.readAsDataURL(file);
            }
        });
    }

    // Avatar edit click
    const avatarEditBtn = document.getElementById('avatarEditBtn');
    if (avatarEditBtn) {
        avatarEditBtn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            document.getElementById('avatarFileInput').click();
        });
    }

    // Avatar file input change - preview
    const avatarFileInput = document.getElementById('avatarFileInput');
    if (avatarFileInput) {
        avatarFileInput.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (file) {
                avatarFile = file;
                const reader = new FileReader();
                reader.onload = function (ev) {
                    let avatarImg = document.getElementById('avatarImage');
                    const placeholder = document.getElementById('avatarPlaceholder');
                    if (placeholder) {
                        // Replace placeholder with a preview img
                        const img = document.createElement('img');
                        img.src = ev.target.result;
                        img.alt = 'Profile';
                        img.className = 'profile-avatar-large';
                        img.id = 'avatarImage';
                        placeholder.parentNode.replaceChild(img, placeholder);
                    } else if (avatarImg) {
                        avatarImg.src = ev.target.result;
                    }
                };
                reader.readAsDataURL(file);
            }
        });
    }

    // Bio edit click - toggle textarea
    const bioEditBtn = document.getElementById('bioEditBtn');
    if (bioEditBtn) {
        bioEditBtn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            const bioDisplay = document.getElementById('profileBioDisplay');
            const bioTextarea = document.getElementById('profileBioTextarea');
            const bioText = document.getElementById('profileBioText');

            if (bioTextarea.style.display === 'none') {
                // Show textarea with current bio
                const currentBio = bioText ? bioText.textContent.trim() : '';
                bioTextarea.value = (currentBio === 'No bio yet.') ? '' : currentBio;
                bioTextarea.style.display = 'block';
                bioDisplay.style.display = 'none';
                bioTextarea.focus();
            } else {
                // Hide textarea, show display
                bioTextarea.style.display = 'none';
                bioDisplay.style.display = '';
            }
        });
    }
});
