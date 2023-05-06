// detach from global
(async () => {
    function makeCopyLink(itemId) {
        let downloadUrl = ApiClient.getItemDownloadUrl(itemId);
        downloadUrl = downloadUrl.replace("/Download?", ".m3u8?");
        downloadUrl = downloadUrl.replace("/Items/", "/PlaylistGen/Items/");

        return downloadUrl;
    }

    function log(...args) {
        console.log("PlaylistGen:", ...args);
    }

    function error(...args) {
        console.error("PlaylistGen:", ...args);
    }

    /**
     *
     * @param {string} itemId the item id
     */
    function injectCopyPlaylistButton(itemId) {
        const actionSheet = document.querySelector(".actionSheetScroller");
        // get last divider
        const allDivider = actionSheet.querySelectorAll(".actionsheetDivider");
        const lastDivider = allDivider[allDivider.length - 1];

        const copyButton = document.createElement("button");
        copyButton.classList.add("listItem", "listItem-button", "actionSheetMenuItem", "emby-button");
        copyButton.setAttribute("is", "emby-button");
        copyButton.setAttribute("type", "button");
        copyButton.setAttribute("data-action", "copyAsPlaylist");

        copyButton.addEventListener("click", () => {
            log("Copying playlist URL to clipboard")
            window.Dashboard.showLoadingMsg();
            const url = makeCopyLink(itemId);
            window.navigator.clipboard.writeText(url).then(() => {
                log("Copied playlist URL to Clipboard")
                window.Dashboard.hideLoadingMsg();
                alert("Copied playlist URL to Clipboard");
            }).catch((err) => {
                window.Dashboard.hideLoadingMsg();
                error(err);
                alert("Failed to copy playlist URL to Clipboard");
            });
        })

        const icon = document.createElement("span");
        icon.className = "actionsheetMenuItemIcon listItemIcon listItemIcon-transparent material-icons copy_all";

        const textParent = document.createElement("div");
        const textChild = document.createElement("div");
        textParent.className = "listItemBody actionsheetListItemBody";
        textChild.className = "listItemBodyText actionSheetItemText";
        textChild.innerText = "Copy as Playlist URL";

        textParent.appendChild(textChild);
        copyButton.appendChild(icon);
        copyButton.appendChild(textParent);

        lastDivider.before(copyButton);
    }

    /**
     *
     * @param {string} itemId the item id
     * @param {string} itemType the item type
     */
    function interceptMoreButton(itemId, itemType) {
        log("PlaylistGen: Intercepting more button...")
        var interceptor = setInterval(() => {
            const actionSheet = document.querySelector(".actionSheetContent");
            if (actionSheet) {
                log("PlaylistGen: Intercepted! Injecting copy button");
                console.log("PlaylistGen: Intercepted! Injecting copy button");
                injectCopyPlaylistButton()
                clearInterval(interceptor);
            }
        }, 100);
    }

    function main() {
        // attach
        log("Attaching handlers...");
        const buttonOverlay = document.querySelectorAll(".cardOverlayButtonIcon.more_vert");
        const detailButton = document.querySelectorAll(".detailButton-icon.more_vert");

        const allowedCopyPlaylistTypes = [
            'BoxSet',
            'CollectionFolder',
            'MusicAlbum',
            'MusicArtist',
            'Playlist',
            'Series',
            'Season'
        ];

        for (let i = 0; i < buttonOverlay.length; i++) {
            const parent = buttonOverlay[i].closest("div.card");
            log(parent);

            const dataId = parent.getAttribute("data-id");
            const dataType = parent.getAttribute("data-type");
            log(dataId, dataType)

            if (allowedCopyPlaylistTypes.indexOf(dataType) === -1) {
                continue;
            }

            // replace event
            buttonOverlay[i].removeEventListener("click", () => {
                interceptMoreButton(dataId)
            });
            buttonOverlay[i].addEventListener("click", () => {
                interceptMoreButton(dataId)
            });
        }

        for (let i = 0; i < detailButton.length; i++) {
            const parent = detailButton[i].closest("button").parentElement.querySelector("[data-type]");
            log(parent)

            const dataId = parent.getAttribute("data-id");
            const dataType = parent.getAttribute("data-type");
            log(dataId, dataType)

            if (allowedCopyPlaylistTypes.indexOf(dataType) === -1) {
                continue;
            }

            // replace event
            detailButton[i].removeEventListener("click", () => {
                interceptMoreButton(dataId)
            });
            detailButton[i].addEventListener("click", () => {
                interceptMoreButton(dataId)
            });
        }

        log("Attached handlers!");
    }

    document.addEventListener('viewshow', main);
    document.addEventListener('pageshow', main);
})();
