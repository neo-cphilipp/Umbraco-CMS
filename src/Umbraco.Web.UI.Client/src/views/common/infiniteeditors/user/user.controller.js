angular.module("umbraco")
  .controller("Umbraco.Editors.UserController", function ($scope, $location, $timeout,
    dashboardResource, userService, historyService, eventsService,
    externalLoginInfoService, authResource,
    currentUserResource, formHelper, localizationService, editorService, twoFactorLoginResource) {

    let vm = this;

    vm.history = historyService.getCurrent();
    vm.showPasswordFields = false;
    vm.changePasswordButtonState = "init";
    vm.hasTwoFactorProviders = false;

    localizationService.localize("general_user").then(function (value) {
      vm.title = value;
    });

    // Set flag if any have deny local login, in which case we must disable all password functionality
    vm.denyLocalLogin = externalLoginInfoService.hasDenyLocalLogin();
    // Only include login providers that have editable options
    vm.externalLoginProviders = externalLoginInfoService.getLoginProvidersWithOptions();

    vm.externalLinkLoginFormAction = Umbraco.Sys.ServerVariables.umbracoUrls.externalLinkLoginsUrl;
    var evts = [];
    evts.push(eventsService.on("historyService.add", function (e, args) {
      vm.history = args.all;
    }));
    evts.push(eventsService.on("historyService.remove", function (e, args) {
      vm.history = args.all;
    }));
    evts.push(eventsService.on("historyService.removeAll", function (e, args) {
      vm.history = [];
    }));

    vm.logout = function () {

      //Add event listener for when there are pending changes on an editor which means our route was not successful
      var pendingChangeEvent = eventsService.on("valFormManager.pendingChanges", function (e, args) {
        //one time listener, remove the event
        pendingChangeEvent();
        vm.close();
      });


      //perform the path change, if it is successful then the promise will resolve otherwise it will fail
      vm.close();
      $location.path("/logout").search('');
    };

    vm.gotoHistory = function (link) {
      $location.path(link);
      vm.close();
    };
    /*
    //Manually update the remaining timeout seconds
    function updateTimeout() {
        $timeout(function () {
            if (vm.remainingAuthSeconds > 0) {
                vm.remainingAuthSeconds--;
                $scope.$digest();
                //recurse
                updateTimeout();
            }

        }, 1000, false); // 1 second, do NOT execute a global digest
    }
    */
    function updateUserInfo() {
      //get the user
      userService.getCurrentUser().then(function (user) {
        vm.user = user;
        if (vm.user) {
          vm.remainingAuthSeconds = vm.user.remainingAuthSeconds;
          vm.canEditProfile = _.indexOf(vm.user.allowedSections, "users") > -1;
          //set the timer
          //updateTimeout();

          currentUserResource.getCurrentUserLinkedLogins().then(function (logins) {

            //reset all to be un-linked
            vm.externalLoginProviders.forEach(provider => provider.linkedProviderKey = undefined);

            //set the linked logins
            for (var login in logins) {
              var found = _.find(vm.externalLoginProviders, function (i) {
                return i.authType == login;
              });
              if (found) {
                found.linkedProviderKey = logins[login];
              }
            }
          });

          //go get the config for the membership provider and add it to the model
          authResource.getPasswordConfig(user.id).then(function (data) {
            vm.changePasswordModel.config = data;
            //ensure the hasPassword config option is set to true (the user of course has a password already assigned)
            //this will ensure the oldPassword is shown so they can change it
            // disable reset password functionality beacuse it does not make sense inside the backoffice
            vm.changePasswordModel.config.hasPassword = true;
            vm.changePasswordModel.config.disableToggle = true;
          });

          twoFactorLoginResource.get2FAProvidersForUser(vm.user.id).then(function (providers) {
            vm.hasTwoFactorProviders = providers.length > 0;
          });

        }
      });


    }

    vm.linkProvider = function (e) {
      e.target.submit();
    }

    vm.unlink = function (e, loginProvider, providerKey) {
      var result = confirm("Are you sure you want to unlink this account?");
      if (!result) {
        e.preventDefault();
        return;
      }

      authResource.unlinkLogin(loginProvider, providerKey).then(function (a, b, c) {
        updateUserInfo();
      });
    }

    //create the initial model for change password
    vm.changePasswordModel = {
      config: {},
      value: {}
    };

    updateUserInfo();


    //remove all event handlers
    $scope.$on('$destroy', function () {
      for (var e = 0; e < evts.length; e++) {
        evts[e]();
      }

    });

    vm.changePassword = function () {

      if (formHelper.submitForm({ scope: $scope })) {

        vm.changePasswordButtonState = "busy";

        currentUserResource.changePassword(vm.changePasswordModel.value).then(function (data) {

          //reset old data
          clearPasswordFields();

          formHelper.resetForm({ scope: $scope });

          vm.changePasswordButtonState = "success";
          $timeout(function () {
            vm.togglePasswordFields();
          }, 2000);

        }, function (err) {
          formHelper.resetForm({ scope: $scope, hasErrors: true });
          formHelper.handleError(err);

          vm.changePasswordButtonState = "error";

        });

      }

    };

    vm.togglePasswordFields = function () {
      clearPasswordFields();
      vm.showPasswordFields = !vm.showPasswordFields;
    }

    function clearPasswordFields() {
      vm.changePasswordModel.value.oldPassword = "";
      vm.changePasswordModel.value.newPassword = "";
      vm.changePasswordModel.value.confirm = "";
    }

    vm.editUser = function () {
      $location
        .path('/users/users/user/' + vm.user.id);
      vm.close();
    }

    vm.toggleConfigureTwoFactor = function () {

      const configureTwoFactorSettings = {
        create: true,
        user: vm.user,
        isCurrentUser: true,// From this view we are always current user (used by the overlay)
        size: "small",
        view: "views/common/infiniteeditors/twofactor/configuretwofactor.html",
        close: function () {
          editorService.close();
        }
      };

      editorService.open(configureTwoFactorSettings);
    }

    vm.close = function () {
      if ($scope.model.close) {
        $scope.model.close();
      }
    }

    dashboardResource.getDashboard("user-dialog").then(function (dashboard) {
      vm.dashboard = dashboard;
    });
  });
