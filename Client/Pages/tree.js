
      google.charts.load('current', {packages:['wordtree']});
      google.charts.setOnLoadCallback(drawChart);

      function drawChart() {
        var data = google.visualization.arrayToDataTable(
          [ ['Phrases'],
            ['men are better than dogs'],
            ['men eat kibble'],
            ['men are better than hamsters'],
            ['men are awesome'],
            ['men are people too'],
            ['men eat mice'],
            ['men meowing'],
            ['men in the cradle'],
            ['men eat mice'],
            ['men in the cradle lyrics'],
            ['men eat kibble'],
            ['men for adoption'],
            ['men are family'],
            ['men eat mice'],
            ['men are better than kittens'],
            ['men are evil'],
            ['men are weird'],
            ['men eat mice'],
          ]
        );

        var options = {
          wordtree: {
            format: 'implicit',
            word: 'men'
          }
        };

        var chart = new google.visualization.WordTree(document.getElementById('wordtree_basic'));
        chart.draw(data, options);
      }
    
