window.taiwuHelper = {
    copyText: async function (text) {
        await navigator.clipboard.writeText(text);
    },
    printRecommendation: function () {
        window.print();
    },
    focusElement: function (id) {
        const element = document.getElementById(id);
        if (element) {
            element.focus();
        }
    }
};
