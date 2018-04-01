"use strict";

(function() {

    /**
     * @ngdoc overview
     * @name ngStorage
     */

    angular.module("ngStorage", []).

    /**
     * @ngdoc object
     * @name ngStorage.$localStorage
     * @requires $rootScope
     * @requires $window
     */

    factory("$localStorage", storageFactory("localStorage")).

    /**
     * @ngdoc object
     * @name ngStorage.$sessionStorage
     * @requires $rootScope
     * @requires $window
     */

    factory("$sessionStorage", storageFactory("sessionStorage"));

    function storageFactory(storageType) {
        return [
            "$rootScope",
            "$window",
            "$log",

            function(
                $rootScope,
                $window,
                $log
            ){
                // #9: Assign a placeholder object if Web Storage is unavailable to prevent breaking the entire AngularJS app
                var webStorage = $window[storageType] || ($log.warn("This browser does not support Web Storage!"), {}),
                    $storage = {
                        $default: function(items) {
                            for (var k in items) {
                                if (items.hasOwnProperty(k)) {
                                    angular.isDefined($storage[k]) || ($storage[k] = items[k]);
                                }
                            }

                            return $storage;
                        },
                        $reset: function(items) {
                            for (var k in $storage) {
                                "$" === k[0] || delete $storage[k];
                            }

                            return $storage.$default(items);
                        }
                    },
                    debounce;

                for (var i = 0, k; i < webStorage.length; i++) {
                    // #8, #10: `webStorage.key(i)` may be an empty string (or throw an exception in IE9 if `webStorage` is empty)
                    (k = webStorage.key(i)) && "ngStorage-" === k.slice(0, 10) && ($storage[k.slice(10)] = angular.fromJson(webStorage.getItem(k)));
                }

                var last$Storage = angular.copy($storage);

                $rootScope.$watch(function() {
                    debounce || (debounce = setTimeout(function() {
                        debounce = null;

                        if (!angular.equals($storage, last$Storage)) {
                            angular.forEach($storage, function(v, k) {
                                angular.isDefined(v) && "$" !== k[0] && webStorage.setItem("ngStorage-" + k, angular.toJson(v));

                                delete last$Storage[k];
                            });

                            for (var k in last$Storage) {
                                webStorage.removeItem("ngStorage-" + k);
                            }

                            last$Storage = angular.copy($storage);
                        }
                    }, 100));
                });

                // #6: Use `$window.addEventListener` instead of `angular.element` to avoid the jQuery-specific `event.originalEvent`
                "localStorage" === storageType && $window.addEventListener && $window.addEventListener("storage", function(event) {
                    if ("ngStorage-" === event.key.slice(0, 10)) {
                        event.newValue ? $storage[event.key.slice(10)] = angular.fromJson(event.newValue) : delete $storage[event.key.slice(10)];

                        last$Storage = angular.copy($storage);

                        $rootScope.$apply();
                    }
                });

                return $storage;
            }
        ];
    }

})();
