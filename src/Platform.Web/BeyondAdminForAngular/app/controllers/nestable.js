app.controller('NestabelCtrl', function ($scope) {
    $scope.mdl = [
      {
          item: { text: 'a' },
          children: []
      },
      {
          item: { text: 'b' },
          children: [
            {
                item: { text: 'c' },
                children: []
            },
            {
                item: { text: 'd' },
                children: []
            }
          ]
      },
      {
          item: { text: 'e' },
          children: []
      },
      {
          item: { text: 'f' },
          children: []
      }
    ];
});