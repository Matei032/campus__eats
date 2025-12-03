// wwwroot/js/stripe-interop.js

window.stripeHelper = {
    stripe: null,
    elements: null,
    cardElement: null,

    // Inițializează Stripe cu Public Key
    initialize: function (publicKey) {
        if (!window.Stripe) {
            console.error('Stripe.js not loaded');
            return false;
        }
        this.stripe = Stripe(publicKey);
        this.elements = this.stripe.elements();
        console.log('✅ Stripe initialized');
        return true;
    },

    // Creează și montează Card Element
    createCardElement: function (elementId) {
        if (!this.elements) {
            console.error('Stripe Elements not initialized');
            return false;
        }

        const style = {
            base: {
                color: '#212529',
                fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto',
                fontSize: '16px',
                '::placeholder': { color: '#6c757d' }
            },
            invalid: {
                color: '#dc3545',
                iconColor: '#dc3545'
            }
        };

        this.cardElement = this.elements.create('card', {
            style: style,
            hidePostalCode: true
        });

        this.cardElement.mount(`#${elementId}`);
        console.log('✅ Card element mounted');
        return true;
    },

    // Confirmă plata cu clientSecret
    confirmCardPayment: async function (clientSecret) {
        if (!this.stripe || !this.cardElement) {
            return { success: false, error: 'Stripe not initialized' };
        }

        try {
            const result = await this.stripe.confirmCardPayment(clientSecret, {
                payment_method: { card: this.cardElement }
            });

            if (result.error) {
                return { success: false, error: result.error.message };
            }

            return { success: true, paymentIntent: result.paymentIntent };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    // Cleanup
    destroy: function () {
        if (this.cardElement) {
            this.cardElement.unmount();
            this.cardElement.destroy();
        }
    }
};