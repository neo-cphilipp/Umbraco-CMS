(function () {
  "use strict";

  function AnalyticsTelemetryController(installerService, analyticResource) {
    vm.validateAndInstall = function () {
      installerService.install();
    };
  }

  let sliderRef = null;

  var vm = this;
  vm.getConsentLevel = getConsentLevel;
  vm.getAllConsentLevels = getAllConsentLevels;
  vm.saveConsentLevel = saveConsentLevel;
  vm.sliderChange = sliderChange;
  vm.setup = setup;
  vm.consentLevel = '';
  vm.consentLevels = [];
  vm.val = 1;
  vm.sliderOptions =
    {
      "start": 1,
      "step": 1,
      "tooltips": [true],
      "format": {
        to: function (value) {
          return vm.consentLevels[value.toFixed(0) - 1];
        },
        from: function (value) {
          return Number(value);
        }
      },
      "range": {
        "min": 1,
        "max": 3
      }
    };
  getAllConsentLevels().then(() => {
    getConsentLevel()
    vm.startPos = calculateStartPositionForSlider();
    vm.sliderVal = vm.consentLevels[vm.startPos];
    vm.sliderOptions.start = vm.startPos;
    vm.val = vm.startPos;
    if (sliderRef) {
      sliderRef.noUiSlider.set(vm.startPos);
    }

  });

  function setup(slider) {
    sliderRef = slider;
  }

  function getConsentLevel() {
    return "Basic";
  }

  function getAllConsentLevels() {
    return analyticResource.getAllConsentLevels().then(function (response) {
      vm.consentLevels = response;
    })
  }

  function sliderChange(values) {
    vm.sliderVal = values[0];
  }

  function calculateStartPositionForSlider() {
    let startPosition = vm.consentLevels.indexOf(vm.consentLevel) + 1;
    if (startPosition === 0) {
      return 2;// Default start value
    }
    return startPosition;
  }

  angular.module("umbraco.install").controller("Umbraco.Install.AnalyticsTelemetryController", AnalyticsController);
})();
