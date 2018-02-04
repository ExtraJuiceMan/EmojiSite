function collapsableClickListener() {
    $(this.parentElement.nextElementSibling).slideToggle(290);
    $(this).first().first().toggleClass("fa-rotate-180");
    return false;
}

function toggleActive() {
    $(this).toggleClass("is-active");
}

function submitFavorite() {
    var a = $(this);
    var innerSpan = $("span:first", this);
    var icon = $("i:first", this);

    $.ajax({
        url: "/favorite",
        type: "POST",
        data: "emojiId=" + a.attr("data-id"),
        statusCode: {
            200: function (result) {
                if (!result.success)
                    return false;

                // Favorite
                if (result.action === 1) {
                    innerSpan.html(parseInt(innerSpan.html()) + 1);
                    icon.removeClass('far');
                    icon.addClass('fas');
                }

                // Unfavorite 
                if (result.action === 2) {
                    innerSpan.html(parseInt(innerSpan.html()) - 1);
                    icon.removeClass('fas');
                    icon.addClass('far');
                }
            },
            429: function (result) {
                return false;
            }
        }
    });

    return false;
}

function submitUnfavorite() {
    var card = $(this).parent().parent().parent();
    $.post("/favorite",
        { emojiId: $(this).attr("data-id") },
        function (result) {
            if (!result.success)
                return false;

            // Unfavorite 
            if (result.action === 2) {
                card.remove();
            }
        });

    return false;
}

function updateSearchBox() {
    if (window.location.href.includes("tags=")) {
        var tags = getParameterByName("tags");
        tags = tags.split(" ");

        var i;
        for (i = 0; i < tags.length; i++) {
            $(".emoji-tag").each(function () {
                if ($(this).attr("data-name") === tags[i]) {
                    $(this).toggleClass("is-active");
                }
            });
        }
    }
    if (window.location.href.includes("keyword=")) {
        var keyword = getParameterByName("keyword");
        $("#searchInput").val(keyword);
    }
    var orderBy = getParameterByName("orderBy");
    var order = getParameterByName("orderType");

    $("#orderAttribute").children("option").each(function () {
        if ($(this).attr("value") == orderBy)
            $(this).attr("selected", "selected")
    })
    $("#orderType").children("option").each(function () {
        if ($(this).attr("value") == orderType)
            $(this).attr("selected", "selected")
    })
}

function resetTags() {
    $(".emoji-tag").each(function () {
        if ($(this).hasClass("is-active")) {
            $(this).removeClass("is-active");
        }
    });
}

function submitSearch() {
    var tags = [];
    $(".emoji-tag").each(function () {
        if ($(this).hasClass("is-active")) {
            tags.push($(this).attr("data-name"));
        }
    });

    $("#tagList").val(tags.join(" "));
    $("#orderBy").val($("#orderAttribute").val());
    $("#order").val($("#orderType").val());
    $("#searchTerm").val($("#searchInput").val());
    $("#search").submit();
}

function onImageError(e) {
    e.onerror = "";
    e.src = "/images/discord-logo.png";
    return true;
}

// https://stackoverflow.com/questions/901115/how-can-i-get-query-string-values-in-javascript
function getParameterByName(name) {
    var url = window.location.href;
    name = name.replace(/[\[\]]/g, "\\$&");
    var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

function loadEvent() {
    $(".info-button").click(collapsableClickListener);
    $(".dropdown").click(toggleActive);
    $(".emoji-tag").click(toggleActive);
    $(".favorite-button").click(submitFavorite);
    $(".unfavorite-button").click(submitUnfavorite);
    $("#submitButton").click(submitSearch);
    $("#resetButton").click(resetTags);
    $("#searchInput").on("keydown", function (e) {
        if (e.which === 13) {
            submitSearch();
        }
    });

    updateSearchBox();
}
window.addEventListener("load", loadEvent);