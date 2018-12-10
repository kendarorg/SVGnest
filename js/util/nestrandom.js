var NestRandom = {
	starter: 0.0001,
	random: function(){
		//return Math.random();
		this.starter+=0.0001;
		return this.starter;
	}
}