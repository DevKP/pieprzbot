<a href="https://gitlab.com/0kitty/persiksharp/-/commits/master"><img alt="pipeline status" src="https://gitlab.com/0kitty/persiksharp/badges/master/pipeline.svg" /></a>



# PieprzBot

Telegram bot to administer my group and does some funny things. The Bot was written using [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) API Client.


### Run in a Docker container

1. Install Docker first:

```
curl -sSL https://get.docker.com/ | sh
```
2. Generate tokens:
 - Telegram API - https://my.telegram.org
 - Clarifai - https://www.clarifai.com
 - AccuWeather - https://developer.accuweather.com

3. Create `tokens.json` in **/Data** folder:

*Template:*
```
{
  "TELEGRAM": [ "123456789:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" ],
  "CLARIFAI": [ "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBB" ],
  "ACCUWEATHER": [ "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC" ],
  "MYSQL" : ["server=localhost;UserId=pieprz;Password=password;database=pieprz;"]
}
```

4. Mount custom **Data** volume and Run:

```
docker run -dit -v /patch/to/data:/bot/Data --restart=always --name=pieprzbot registry.gitlab.com/0kitty/persiksharp
```


## Built With

* [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) - .NET Client for Telegram Bot API
* [log4net](http://logging.apache.org/log4net/) - Logger
* [GitInfo](https://github.com/kzu/GitInfo) - Git and SemVer Info from MSBuild, C# and VB


## Authors

* **Vladislav Kotikovich** - [0Kitty](https://gitlab.com/0kitty)
* **Pickle** - [Dark Pickle](https://gitlab.com/00Pickle00)
* **Dmitry Skrylnikov** [skrylnikov](https://gitlab.com/skrylnikov)


## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

