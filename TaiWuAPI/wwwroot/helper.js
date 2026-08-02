window.taiwuHelper = {
    copyText: async function (text) {
        await navigator.clipboard.writeText(text);
    },
    printRecommendation: function () {
        window.print();
    }
};
