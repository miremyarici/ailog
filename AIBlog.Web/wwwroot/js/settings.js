// Settings Page JavaScript
document.addEventListener('DOMContentLoaded', function () {
    // ========================================
    // API Helper Function
    // ========================================
    async function apiCall(endpoint, data) {
        try {
            const response = await fetch(`/Settings/${endpoint}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });
            return await response.json();
        } catch (error) {
            console.error('API Error:', error);
            return { success: false };
        }
    }

    // ========================================
    // Modal Functions
    // ========================================
    function openModal(modalId) {
        document.getElementById(modalId).classList.add('active');
    }

    function closeModal(modalId) {
        document.getElementById(modalId).classList.remove('active');
    }

    // Close modals when clicking overlay
    document.querySelectorAll('.modal-overlay').forEach(overlay => {
        overlay.addEventListener('click', function () {
            this.closest('.settings-modal').classList.remove('active');
        });
    });

    // Close modals with cancel buttons
    document.querySelectorAll('.modal-cancel-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            this.closest('.settings-modal').classList.remove('active');
        });
    });

    // ========================================
    // Change Buttons (Email, Phone, Password)
    // ========================================
    const changeBtns = document.querySelectorAll('.change-btn');
    changeBtns.forEach((btn, index) => {
        btn.addEventListener('click', function () {
            if (index === 0) {
                openModal('emailModal');
            } else if (index === 1) {
                openModal('phoneModal');
            } else if (index === 2) {
                openModal('passwordModal');
            }
        });
    });

    // Save email - with database update
    document.getElementById('saveEmail')?.addEventListener('click', async function () {
        const newEmail = document.getElementById('newEmailInput').value;
        if (!newEmail || !newEmail.includes('@')) {
            alert('Please enter a valid email address.');
            return;
        }

        const result = await apiCall('UpdateEmail', { email: newEmail });
        if (result.success) {
            alert('Email updated successfully!');
            closeModal('emailModal');
            location.reload();
        } else {
            alert('Failed to update email. Please try again.');
        }
    });

    // Save phone - with database update
    document.getElementById('savePhone')?.addEventListener('click', async function () {
        const newPhone = document.getElementById('newPhoneInput').value;
        if (!newPhone || newPhone.length < 10) {
            alert('Please enter a valid phone number.');
            return;
        }

        const result = await apiCall('UpdatePhone', { phone: newPhone });
        if (result.success) {
            alert('Phone number updated successfully!');
            closeModal('phoneModal');
            location.reload();
        } else {
            alert('Failed to update phone number. Please try again.');
        }
    });

    // Save password - with database update
    document.getElementById('savePassword')?.addEventListener('click', async function () {
        const currentPass = document.getElementById('currentPasswordInput').value;
        const newPass = document.getElementById('newPasswordInput').value;
        const confirmPass = document.getElementById('confirmPasswordInput').value;

        if (!currentPass || !newPass || !confirmPass) {
            alert('Please fill in all fields.');
            return;
        }
        if (newPass !== confirmPass) {
            alert('New passwords do not match.');
            return;
        }
        if (newPass.length < 6) {
            alert('Password must be at least 6 characters.');
            return;
        }

        const result = await apiCall('UpdatePassword', {
            currentPassword: currentPass,
            newPassword: newPass
        });

        if (result.success) {
            alert('Password updated successfully!');
            closeModal('passwordModal');
            document.getElementById('currentPasswordInput').value = '';
            document.getElementById('newPasswordInput').value = '';
            document.getElementById('confirmPasswordInput').value = '';
        } else {
            alert('Failed to update password. Please try again.');
        }
    });

    // ========================================
    // Theme Buttons
    // ========================================
    const themeBtns = document.querySelectorAll('.theme-btn');
    themeBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            themeBtns.forEach(b => b.classList.remove('active'));
            this.classList.add('active');

            const theme = this.dataset.theme;
            document.body.setAttribute('data-theme', theme);
            localStorage.setItem('theme', theme);
        });
    });

    // Load saved theme
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme) {
        themeBtns.forEach(btn => {
            btn.classList.remove('active');
            if (btn.dataset.theme === savedTheme) {
                btn.classList.add('active');
            }
        });
    }

    // ========================================
    // Interest Tags - with database update
    // ========================================
    const interestTags = document.querySelectorAll('.interest-tag');

    // Function to save interests to database
    async function saveInterests() {
        const selectedIds = [];
        interestTags.forEach(tag => {
            if (tag.classList.contains('selected')) {
                const categoryId = tag.dataset.categoryId;
                if (categoryId) {
                    selectedIds.push(parseInt(categoryId));
                }
            }
        });

        const result = await apiCall('UpdateInterests', { categoryIds: selectedIds });
        if (!result.success) {
            console.error('Failed to save interests');
        }
    }

    // Debounce function to avoid too many API calls
    let saveTimeout = null;
    function debouncedSaveInterests() {
        if (saveTimeout) clearTimeout(saveTimeout);
        saveTimeout = setTimeout(saveInterests, 500);
    }

    interestTags.forEach(tag => {
        tag.addEventListener('click', function () {
            this.classList.toggle('selected');
            debouncedSaveInterests();
        });
    });

    // Clear all interests
    document.querySelector('.clear-all-btn')?.addEventListener('click', async function () {
        interestTags.forEach(tag => tag.classList.remove('selected'));
        await saveInterests();
    });

    // ========================================
    // Profile Visibility Buttons - with database update
    // ========================================
    const visibilityBtns = document.querySelectorAll('.visibility-btn');
    visibilityBtns.forEach(btn => {
        btn.addEventListener('click', async function () {
            const visibility = this.dataset.visibility;

            const result = await apiCall('UpdateProfileVisibility', { visibility: visibility });
            if (result.success) {
                visibilityBtns.forEach(b => b.classList.remove('active'));
                this.classList.add('active');
            } else {
                alert('Failed to update profile visibility.');
            }
        });
    });

    // ========================================
    // Search Engine Visibility Toggle - with database update
    // ========================================
    const searchVisibilityToggles = document.querySelectorAll('.privacy-row:nth-child(2) .toggle input');
    // Get the specific search visibility toggle
    const privacyRows = document.querySelectorAll('.privacy-group:nth-child(2) .privacy-row');
    privacyRows.forEach(row => {
        const label = row.querySelector('span');
        if (label && label.textContent.includes('Search Engine Visibility')) {
            const toggle = row.querySelector('input[type="checkbox"]');
            if (toggle) {
                toggle.addEventListener('change', async function () {
                    const result = await apiCall('UpdateSearchVisibility', { visible: this.checked });
                    if (!result.success) {
                        this.checked = !this.checked; // Revert on failure
                        alert('Failed to update search visibility.');
                    }
                });
            }
        }
    });

    // ========================================
    // Two-Factor Authentication with Real TOTP
    // ========================================
    let currentTOTPSecret = null;
    let qrCodeInstance = null;

    // Generate random Base32 secret (for TOTP)
    function generateSecret() {
        const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
        let secret = '';
        for (let i = 0; i < 16; i++) {
            secret += chars.charAt(Math.floor(Math.random() * chars.length));
        }
        return secret;
    }

    // Generate TOTP code from secret
    function generateTOTP(secret) {
        const base32Chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
        let bits = '';
        for (let i = 0; i < secret.length; i++) {
            const val = base32Chars.indexOf(secret[i].toUpperCase());
            if (val >= 0) {
                bits += val.toString(2).padStart(5, '0');
            }
        }

        const bytes = [];
        for (let i = 0; i < bits.length; i += 8) {
            bytes.push(parseInt(bits.substr(i, 8), 2));
        }
        const key = new Uint8Array(bytes);

        const counter = Math.floor(Date.now() / 30000);
        const counterBytes = new Uint8Array(8);
        let temp = counter;
        for (let i = 7; i >= 0; i--) {
            counterBytes[i] = temp & 0xff;
            temp = Math.floor(temp / 256);
        }

        return hmacSha1(key, counterBytes).then(hash => {
            const offset = hash[19] & 0xf;
            const code = ((hash[offset] & 0x7f) << 24 |
                (hash[offset + 1] & 0xff) << 16 |
                (hash[offset + 2] & 0xff) << 8 |
                (hash[offset + 3] & 0xff)) % 1000000;
            return code.toString().padStart(6, '0');
        });
    }

    async function hmacSha1(key, data) {
        const cryptoKey = await crypto.subtle.importKey(
            'raw', key, { name: 'HMAC', hash: 'SHA-1' }, false, ['sign']
        );
        const signature = await crypto.subtle.sign('HMAC', cryptoKey, data);
        return new Uint8Array(signature);
    }

    const twoFactorToggle = document.getElementById('twoFactorToggle');
    if (twoFactorToggle) {
        twoFactorToggle.addEventListener('change', function () {
            if (this.checked) {
                currentTOTPSecret = generateSecret();

                const issuer = 'AIBlog';
                const accountName = 'user@ailog.com';
                const totpUri = `otpauth://totp/${issuer}:${accountName}?secret=${currentTOTPSecret}&issuer=${issuer}&algorithm=SHA1&digits=6&period=30`;

                const qrContainer = document.getElementById('qrCodeContainer');
                qrContainer.innerHTML = '';

                qrCodeInstance = new QRCode(qrContainer, {
                    text: totpUri,
                    width: 150,
                    height: 150,
                    colorDark: '#000000',
                    colorLight: '#ffffff',
                    correctLevel: QRCode.CorrectLevel.M
                });

                document.getElementById('secretKeyDisplay').textContent = currentTOTPSecret;
                openModal('twoFactorModal');
            } else {
                // Disable 2FA
                apiCall('UpdateTwoFactor', { enabled: false }).then(result => {
                    if (!result.success) {
                        twoFactorToggle.checked = true;
                        alert('Failed to disable 2FA.');
                    }
                });
            }
        });
    }

    // Cancel 2FA setup
    document.getElementById('cancel2FA')?.addEventListener('click', function () {
        const toggle = document.getElementById('twoFactorToggle');
        if (toggle) toggle.checked = false;
        currentTOTPSecret = null;
        closeModal('twoFactorModal');
    });

    // Confirm 2FA setup - verify and save to database
    document.getElementById('confirm2FA')?.addEventListener('click', async function () {
        const enteredCode = document.getElementById('twoFactorCode').value;

        if (!enteredCode || enteredCode.length !== 6) {
            alert('Please enter a valid 6-digit code.');
            return;
        }

        if (!currentTOTPSecret) {
            alert('Error: No secret generated. Please try again.');
            return;
        }

        const expectedCode = await generateTOTP(currentTOTPSecret);

        if (enteredCode === expectedCode) {
            // Save 2FA enabled status to database
            const result = await apiCall('UpdateTwoFactor', { enabled: true });
            if (result.success) {
                alert('Two-Factor Authentication enabled successfully!');
                closeModal('twoFactorModal');
                document.getElementById('twoFactorCode').value = '';
            } else {
                alert('Failed to save 2FA settings.');
                document.getElementById('twoFactorToggle').checked = false;
            }
        } else {
            alert('Invalid verification code. Please check your authenticator app and try again.');
        }
    });

    // ========================================
    // Data Actions
    // ========================================
    document.querySelector('.download-btn')?.addEventListener('click', function () {
        alert('Your data download will be ready shortly. We will email you when it is available.');
    });

    document.querySelector('.delete-btn')?.addEventListener('click', function () {
        if (confirm('Are you sure you want to delete your account? This action cannot be undone.')) {
            alert('Account deletion request submitted. You will be logged out shortly.');
        }
    });
});
