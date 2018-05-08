var width = 500,
    height = 500,
    radius = Math.min(width, height) / 2,
    innerRadius = 0.3 * radius;

var pie = d3.layout.pie()
    .sort(null)
    .value(function (d) { return d.width; });

var tip = d3.tip()
    .attr('class', 'd3-tip')
    .offset([0, 0])
    .html(function (d) {
        return d.data.label + ": <span style='color:orangered'>" + d.data.score + "</span>";
    });

var arc = d3.svg.arc()
    .innerRadius(innerRadius)
    .outerRadius(function (d) {
        return (radius - innerRadius) * (d.data.score / 100.0) + innerRadius;
    });

var color = d3.scale.linear()
    .domain([0, 50, 80, 100])
    .range(["red", "orange", "yellow", "green"]);

var outlineArc = d3.svg.arc()
    .innerRadius(innerRadius)
    .outerRadius(radius);

var svg = d3.select("body").append("svg")
    .attr("width", width)
    .attr("height", height)
    .append("g")
    .attr("transform", "translate(" + width / 2 + "," + height / 2 + ")");

svg.call(tip);

d3.csv('Test_Data/aster_data.csv', function (error, data) {

    data.forEach(function (d) {
        d.id = d.id;
        d.order = +d.order;
        d.color = color(d.score);
        d.weight = +d.weight;
        d.score = +d.score;
        d.width = +d.weight;
        d.label = d.label;
    });

    var path = svg.selectAll(".solidArc")
        .data(pie(data))
        .enter().append("path")
        .attr("fill", function (d) { return d.data.color; })
        .attr("class", "solidArc")
        .attr("stroke", "gray")
        .attr("d", arc)
        .on('mouseover', tip.show)
        .on('mouseout', tip.hide);

    var outerPath = svg.selectAll(".outlineArc")
        .data(pie(data))
        .enter().append("path")
        .attr("fill", "none")
        .attr("stroke", "gray")
        .attr("class", "outlineArc")
        .attr("d", outlineArc);

    // calculate the line OEE
    //var score_temp;
    //var score = data.forEach(function (d) {
    //    return score_temp = d * score_temp
    //});
    var score = data.reduce(function (a, b) {
        if (a == 0) {
            return (b.score/100);
        }
        else {
            return a * (b.score/100);
        }
    }, 0);
            ///
        //data.reduce(function (a) {
        //    return a + 1;
        //}, 0);

    svg.append("svg:text")
        .attr("class", "aster-score")
        .attr("dy", ".35em")
        .attr("text-anchor", "middle") // text-align: right
        .text("Line OEE: " + score.toPrecision(4) + "%");
});