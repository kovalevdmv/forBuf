package main

import (
	"encoding/base64"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"os"
	"regexp"
	"strings"
)

func main() {

	fmt.Println("listen port 80")

	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {

		if r.Method == "GET" {

			key := r.URL.Query().Get("key")
			if key != "" {
				dat, err := os.ReadFile(key)
				if err != nil {
					fmt.Fprintf(w, base64.StdEncoding.EncodeToString([]byte("null")))
				}
				fmt.Fprintf(w, base64.StdEncoding.EncodeToString([]byte(string(dat))))
				log.Println("ok get for key=" + key)
			}

			curVer := r.URL.Query().Get("curVer")
			if curVer != "" {
				dat, err := os.ReadFile("curVer")
				if err != nil {
					fmt.Fprintf(w, base64.StdEncoding.EncodeToString([]byte("null")))
				}
				_ver := base64.StdEncoding.EncodeToString([]byte(string(dat)))
				fmt.Fprintf(w, _ver)
				log.Println("ok get curVer=" + _ver)
			}
			return
		}

		body, _ := ioutil.ReadAll(r.Body)
		str := string(body)

		re, _ := regexp.Compile(`key=.*?,`)
		res := re.FindAllString(str, -1)
		key := strings.Replace(res[0], "key=", "", 1)
		key = strings.Replace(key, ",", "", 1)

		os.WriteFile(key, body, 0644)
		log.Println(key)
		log.Println("save file for key=" + key)

		fmt.Fprintf(w, "ok")

	})
	http.ListenAndServe(":80", nil)
}
