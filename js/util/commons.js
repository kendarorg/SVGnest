function Commons() {
    this.shuffle = function(array) {
        var currentIndex = array.length,
            temporaryValue, randomIndex;

        // While there remain elements to shuffle...
        while (0 !== currentIndex) {

            // Pick a remaining element...
            randomIndex = Math.floor(NestRandom.random() * currentIndex);
            currentIndex -= 1;

            // And swap it with the current element.
            temporaryValue = array[currentIndex];
            array[currentIndex] = array[randomIndex];
            array[randomIndex] = temporaryValue;
        }

        return array;
    }

    this.rotatePolygon = function(polygon, degrees) {
        var rotated = GeometryUtil.rotatePolygon(polygon, degrees);
        /*[];
        		angle = degrees * Math.PI / 180;
        		for(var i=0; i<polygon.length; i++){
        			var x = polygon[i].x;
        			var y = polygon[i].y;
        			var x1 = x*Math.cos(angle)-y*Math.sin(angle);
        			var y1 = x*Math.sin(angle)+y*Math.cos(angle);
        							
        			rotated.push({x:x1, y:y1});
        		}*/

        if (polygon.children && polygon.children.length > 0) {
            rotated.children = [];
            for (var j = 0; j < polygon.children.length; j++) {
                rotated.children.push(this.rotatePolygon(polygon.children[j], degrees));
            }
        }

        return rotated;
    };

    this.toClipperCoordinates = function(polygon) {
        var clone = [];
        for (var i = 0; i < polygon.length; i++) {
            clone.push({
                X: polygon[i].x,
                Y: polygon[i].y
            });
        }

        return clone;
    };


    this.toNestCoordinates = function(polygon, scale) {
        var clone = [];
        for (var i = 0; i < polygon.length; i++) {
            clone.push({
                x: polygon[i].X / scale,
                y: polygon[i].Y / scale
            });
        }

        return clone;
    };
    this.flatten=function(obj) {
    	if(obj==null)return obj;
	    var result = Object.create(obj);
	    for(var key in result) {
	    	if((result[key] !== Object(result[key]))||(result[key]==="undefined" || result[key]==undefined || result[key]==null)){
	        	result[key] = result[key];
	    	}else{
	        	result[key] = this.flatten(result[key]);
	        }
	    }
	    return result;
	}
    this.log = function() {
            if (typeof console !== "undefined") {
            	var ars= "";
            	for(var i=0;i<arguments.length;i++){
            		if(arguments[i]==="undefined" || arguments[i]==undefined || arguments[i]==null){
            			ars+="null"
            		}else if((arguments[i] !== Object(arguments[i]))){
        				ars+=arguments[i]+"";
        			}else{
        				ars+=JSON.stringify(this.flatten(arguments[i]));
        			}
        			
            		if(i<arguments.length-1){ars+=",\r\n";}
            	}
                console.log(ars);
            }
        }
}