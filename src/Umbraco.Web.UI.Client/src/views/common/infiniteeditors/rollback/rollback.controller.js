(function () {
    "use strict";

    function RollbackController($scope, $timeout, contentResource, localizationService, assetsService, dateHelper, userService) {

        var vm = this;

        vm.rollback = rollback;
        vm.changeLanguage = changeLanguage;
        vm.changeVersion = changeVersion;
        vm.submit = submit;
        vm.close = close;
        vm.pinVersion = pinVersion;
        vm.goToPage = goToPage;

        //////////

        function onInit() {

            vm.loading = true;
            vm.variantVersions = [];
            vm.diff = null;
            vm.currentVersion = null;
            vm.rollbackButtonDisabled = true;
            vm.labels = {};

            // TODO: implement
            vm.itemsPerPage = 10;
            vm.totalPages = 10;
            vm.currentPage = 1;

            // find the current version for invariant nodes
            if($scope.model.node.variants.length === 1) {
                vm.currentVersion = $scope.model.node.variants[0];
            }

            // find the current version for nodes with variants
            if($scope.model.node.variants.length > 1) {
                var active = _.find($scope.model.node.variants, function (v) {
                    return v.active;
                });

                // preselect the language in the dropdown
                if(active) {
                    vm.selectedLanguage = active;
                    vm.currentVersion = active;
                }
            }

            localizationService.localizeMany(["actions_rollback", "general_choose"]).then(function (data) {
                // set default title
                if (!$scope.model.title) {
                    $scope.model.title = data[0];
                }
                vm.labels.choose = data[1];
            });

            // Load in diff library
            assetsService.loadJs('lib/jsdiff/diff.min.js', $scope).then(function () {

                getVersions().then(function(){
                    vm.loading = false;
                });

            });

        }

        function changeLanguage(language) {
            vm.currentVersion = language;
            getVersions();
        }

        function changeVersion(version) {

            if (vm.previousVersion && version && vm.previousVersion.versionId === version.versionId) {
                vm.previousVersion = null;
                vm.diff = null;
                return;
            }

            if (version && version.versionId) {
                vm.loadingDiff = true;
                const culture = $scope.model.node.variants.length > 1 ? vm.currentVersion.language.culture : null;

                contentResource.getRollbackVersion(version.versionId, culture)
                    .then(function(data) {
                        vm.previousVersion = data;
                        vm.previousVersion.versionId = version.versionId;
                        vm.previousVersion.displayValue = version.displayValue;
                        createDiff(vm.currentVersion, vm.previousVersion);

                        vm.loadingDiff = false;
                        vm.rollbackButtonDisabled = false;
                    }, function () {
                        vm.loadingDiff = false;
                    });

            } else {
                vm.diff = null;
                vm.rollbackButtonDisabled = true;
            }
        }

        function getVersions() {

            const nodeId = $scope.model.node.id;
            const culture = $scope.model.node.variants.length > 1 ? vm.currentVersion.language.culture : null;

            return contentResource.getRollbackVersions(nodeId, culture)
                .then(function (data) {
                    // get current backoffice user and format dates
                    userService.getCurrentUser().then(function (currentUser) {
                        vm.previousVersions = data.map(version => {
                            var timestampFormatted = dateHelper.getLocalDate(version.versionDate, currentUser.locale, 'LLL');
                            version.displayValue = timestampFormatted + ' - ' + version.versionAuthorName;
                            return version;
                        });
                    });
                });
        }

        /**
         * This will load in a new version
         */
        function createDiff(currentVersion, previousVersion) {
            vm.diff = {};
            vm.diff.properties = [];

            // find diff in name
            vm.diff.name = JsDiff.diffWords(currentVersion.name, previousVersion.name);

            // extract all properties from the tabs and create new object for the diff
            currentVersion.tabs.forEach(function (tab) {
                tab.properties.forEach(function (property) {
                    let oldTabIndex = -1;
                    let oldTabPropertyIndex = -1;
                    const previousVersionTabs = previousVersion.tabs;

                    // find the property by alias, but only search until we find it
                    for (var oti = 0, length = previousVersionTabs.length; oti < length; oti++) {
                        const opi = previousVersionTabs[oti].properties.findIndex(p => p.alias === property.alias);
                        if (opi !== -1) {
                            oldTabIndex = oti;
                            oldTabPropertyIndex = opi;
                            break;
                        }
                    }

                    if (oldTabIndex !== -1 && oldTabPropertyIndex !== -1) {
                        let oldProperty = previousVersion.tabs[oldTabIndex].properties[oldTabPropertyIndex];

                        // copy existing properties, so it doesn't manipulate existing properties on page
                        oldProperty = Utilities.copy(oldProperty);
                        property = Utilities.copy(property);
                        
                        // we have to make properties storing values as object into strings (Grid, nested content, etc.)
                        if (property.value instanceof Object) {
                            property.value = JSON.stringify(property.value, null, 1);
                            property.isObject = true;
                        }

                        if (oldProperty.value instanceof Object) {
                            oldProperty.value = JSON.stringify(oldProperty.value, null, 1);
                            oldProperty.isObject = true;
                        }

                        // diff requires a string
                        property.value = property.value ? property.value + '' : '';
                        oldProperty.value = oldProperty.value ? oldProperty.value + '' : '';
                        
                        const diffProperty = {
                            'alias': property.alias,
                            'label': property.label,
                            'diff': property.isObject ? JsDiff.diffJson(property.value, oldProperty.value) : JsDiff.diffWords(property.value, oldProperty.value),
                            'isObject': property.isObject || oldProperty.isObject
                        };
                        
                        vm.diff.properties.push(diffProperty);
                    }
                });
            });
        }

        function rollback() {

            vm.rollbackButtonState = "busy";

            const nodeId = $scope.model.node.id;
            const versionId = vm.previousVersion.versionId;
            const culture = $scope.model.node.variants.length > 1 ? vm.currentVersion.language.culture : null;

            return contentResource.rollback(nodeId, versionId, culture)
                .then(data => {
                    vm.rollbackButtonState = "success";
                    submit();
                }, error => {
                    vm.rollbackButtonState = "error";
                });

        }

        function submit() {
            if($scope.model.submit) {
                $scope.model.submit($scope.model.submit);
            }
        }

        function close() {
            if($scope.model.close) {
                $scope.model.close();
            }
        }

        // TODO: implement
        function pinVersion (version) {
            version.pinningState = 'busy';

            $timeout(() => {
                version.pinningState = 'init';
                version.isPinned = !version.isPinned;
            }, 2000);
        }

        // TODO: implement
        function goToPage (pageNumber) {
            console.log('PAGE NUMBER', pageNumber);
        }

        onInit();

    }

    angular.module("umbraco").controller("Umbraco.Editors.RollbackController", RollbackController);

})();
